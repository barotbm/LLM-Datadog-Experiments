using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Models;
using LLMJudgePipeline.Services.Judges;

namespace LLMJudgePipeline.Services.Routing;

public class RiskBasedJudgeRouter : IJudgeRouter
{
    private readonly ExperimentAwareJudge _experimentAwareJudge;
    private readonly IDatadogExperiments _experiments;
    private readonly IDatadogMetrics _metrics;
    private readonly ILogger<RiskBasedJudgeRouter> _logger;

    private static readonly HashSet<string> SensitiveIntents = new()
    {
        "PayoffQuote",
        "Payment",
        "AddressChange"
    };

    public RiskBasedJudgeRouter(
        ExperimentAwareJudge experimentAwareJudge,
        IDatadogExperiments experiments,
        IDatadogMetrics metrics,
        ILogger<RiskBasedJudgeRouter> logger)
    {
        _experimentAwareJudge = experimentAwareJudge;
        _experiments = experiments;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<JudgeResult> RouteAsync(string userMessage, GeneratorResult output, string sessionId)
    {
        // Get the routing strategy from Datadog experiments
        var routingStrategy = _experiments.GetVariant("judge-routing-strategy", sessionId);

        _logger.LogInformation("Routing strategy: {Strategy} for intent: {Intent}", routingStrategy, output.Intent);

        bool shouldEvaluate = routingStrategy switch
        {
            "always" => true,
            "sensitive-only" => IsSensitiveIntent(output.Intent),
            "low-confidence" => output.Confidence < 0.75,
            _ => false // "never" or unknown
        };

        if (shouldEvaluate)
        {
            _logger.LogInformation("Judge evaluation triggered: Strategy={Strategy}, Intent={Intent}, Confidence={Confidence}",
                routingStrategy, output.Intent, output.Confidence);

            _metrics.Increment("llm.judge.invoked", 1, new[]
            {
                $"strategy:{routingStrategy}",
                $"intent:{output.Intent}"
            });

            return await _experimentAwareJudge.EvaluateWithSessionAsync(userMessage, output, sessionId);
        }
        else
        {
            _logger.LogInformation("Judge evaluation skipped: Strategy={Strategy}, Intent={Intent}, Confidence={Confidence}",
                routingStrategy, output.Intent, output.Confidence);

            _metrics.Increment("llm.judge.skipped", 1, new[]
            {
                $"strategy:{routingStrategy}",
                $"intent:{output.Intent}"
            });

            // Return synthetic passing result when judge is skipped
            return new JudgeResult
            {
                IsValid = true,
                Violation = "none",
                Confidence = 1.0,
                JudgeModel = "skipped",
                LatencyMs = 0,
                WasSkipped = true,
                Reason = $"Skipped due to routing strategy: {routingStrategy}"
            };
        }
    }

    public bool ShouldEvaluate(GeneratorResult output)
    {
        // This method can be used for quick checks without session context
        // Default to conservative approach
        return IsSensitiveIntent(output.Intent) || output.Confidence < 0.75;
    }

    private bool IsSensitiveIntent(string intent)
    {
        return SensitiveIntents.Contains(intent);
    }
}
