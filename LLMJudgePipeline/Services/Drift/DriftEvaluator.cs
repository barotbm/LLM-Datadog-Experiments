using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Models;

namespace LLMJudgePipeline.Services.Drift;

public class DriftEvaluator : IDriftEvaluator
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DriftEvaluator> _logger;

    private const double SimilarityThreshold = 0.75;
    private const int MaxHistorySize = 3;

    private static readonly Dictionary<string, HashSet<string>> ValidDomains = new()
    {
        { "loan_servicing", new HashSet<string> { "PayoffQuote", "Payment", "BalanceInquiry", "RateInformation" } },
        { "account_management", new HashSet<string> { "AddressChange", "ContactUpdate", "AccountInquiry" } },
        { "general", new HashSet<string> { "GeneralInquiry", "Help", "Other" } }
    };

    private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new()
    {
        { "PayoffQuote", new HashSet<string> { "Payment", "BalanceInquiry", "GeneralInquiry" } },
        { "Payment", new HashSet<string> { "PayoffQuote", "BalanceInquiry", "GeneralInquiry" } },
        { "BalanceInquiry", new HashSet<string> { "Payment", "PayoffQuote", "RateInformation" } },
        { "AddressChange", new HashSet<string> { "GeneralInquiry", "ContactUpdate" } },
        { "GeneralInquiry", new HashSet<string> { "PayoffQuote", "Payment", "BalanceInquiry", "AddressChange" } }
    };

    public DriftEvaluator(IEmbeddingService embeddingService, ILogger<DriftEvaluator> logger)
    {
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<DriftEvalResult> EvaluateAsync(
        string userMessage, 
        GeneratorResult output, 
        List<string>? recentIntents = null)
    {
        var result = new DriftEvalResult();
        var details = new List<string>();

        try
        {
            // 1. Calculate embedding similarity between user message and predicted intent
            var intentDescription = GetIntentDescription(output.Intent);
            result.Similarity = await _embeddingService.CalculateCosineSimilarityAsync(userMessage, intentDescription);

            if (result.Similarity < SimilarityThreshold)
            {
                details.Add($"Low similarity: {result.Similarity:F3} < {SimilarityThreshold}");
            }

            // 2. Check domain consistency
            result.DomainMismatch = CheckDomainMismatch(output.Intent, recentIntents);
            if (result.DomainMismatch)
            {
                details.Add("Domain mismatch detected");
            }

            // 3. Check state machine validity
            result.InvalidStateTransition = CheckInvalidStateTransition(output.Intent, recentIntents);
            if (result.InvalidStateTransition)
            {
                details.Add("Invalid state transition detected");
            }

            // Overall drift detection
            result.DriftDetected = result.Similarity < SimilarityThreshold 
                                  || result.DomainMismatch 
                                  || result.InvalidStateTransition;

            result.Details = details.Count > 0 ? string.Join("; ", details) : "No drift detected";

            _logger.LogInformation(
                "Drift evaluation: DriftDetected={DriftDetected}, Similarity={Similarity:F3}, DomainMismatch={DomainMismatch}, InvalidTransition={InvalidTransition}",
                result.DriftDetected, result.Similarity, result.DomainMismatch, result.InvalidStateTransition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during drift evaluation");
            result.DriftDetected = false; // Fail open
            result.Details = $"Error: {ex.Message}";
        }

        return result;
    }

    private string GetIntentDescription(string intent)
    {
        return intent switch
        {
            "PayoffQuote" => "User wants to know the loan payoff amount",
            "Payment" => "User wants to make a payment on their loan",
            "AddressChange" => "User wants to update their mailing address",
            "BalanceInquiry" => "User asks about their current loan balance",
            "RateInformation" => "User asks about interest rates",
            "GeneralInquiry" => "User has a general question",
            _ => "General user inquiry"
        };
    }

    private bool CheckDomainMismatch(string currentIntent, List<string>? recentIntents)
    {
        if (recentIntents == null || recentIntents.Count == 0)
        {
            return false;
        }

        var currentDomain = GetDomain(currentIntent);
        var recentDomains = recentIntents.Select(GetDomain).Distinct().ToList();

        // Check if we're jumping between unrelated domains
        if (currentDomain == "general")
        {
            return false; // General is always allowed
        }

        foreach (var recentDomain in recentDomains)
        {
            if (recentDomain != "general" && recentDomain != currentDomain)
            {
                return true; // Domain mismatch
            }
        }

        return false;
    }

    private bool CheckInvalidStateTransition(string currentIntent, List<string>? recentIntents)
    {
        if (recentIntents == null || recentIntents.Count == 0)
        {
            return false; // First interaction, no transition to validate
        }

        var lastIntent = recentIntents.LastOrDefault();
        if (string.IsNullOrEmpty(lastIntent))
        {
            return false;
        }

        // Check if transition is valid
        if (ValidTransitions.TryGetValue(lastIntent, out var validNextIntents))
        {
            if (!validNextIntents.Contains(currentIntent) && currentIntent != "Other")
            {
                _logger.LogWarning(
                    "Invalid state transition detected: {LastIntent} -> {CurrentIntent}",
                    lastIntent, currentIntent);
                return true;
            }
        }

        return false;
    }

    private string GetDomain(string intent)
    {
        foreach (var domain in ValidDomains)
        {
            if (domain.Value.Contains(intent))
            {
                return domain.Key;
            }
        }
        return "general";
    }
}
