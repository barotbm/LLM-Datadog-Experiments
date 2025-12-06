using LLMJudgePipeline.Interfaces;

namespace LLMJudgePipeline.Infrastructure;

/// <summary>
/// Mock implementation of Datadog Experiments/Feature Flags
/// In production, replace with actual Datadog Feature Flags SDK
/// </summary>
public class DatadogExperimentsService : IDatadogExperiments
{
    private readonly ILogger<DatadogExperimentsService> _logger;
    private readonly IConfiguration _configuration;

    public DatadogExperimentsService(ILogger<DatadogExperimentsService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public string GetVariant(string experimentName, string sessionId)
    {
        // In production, this would call Datadog Feature Flags API
        // For now, we'll use configuration-based defaults with simple hashing for distribution

        var defaultVariant = experimentName switch
        {
            "llm-judge-model" => _configuration["Experiments:JudgeModel"] ?? "gpt4o-mini",
            "judge-routing-strategy" => _configuration["Experiments:RoutingStrategy"] ?? "sensitive-only",
            _ => "default"
        };

        // Simple hash-based distribution for demonstration
        var hash = Math.Abs(sessionId.GetHashCode());
        
        if (experimentName == "llm-judge-model")
        {
            var variants = new[] { "gpt4o-mini", "gpt41-mini", "gpt4o-strong" };
            var variant = variants[hash % variants.Length];
            
            _logger.LogInformation("Experiment {ExperimentName}: Selected variant {Variant} for session {SessionId}",
                experimentName, variant, sessionId);
            
            return variant;
        }
        
        if (experimentName == "judge-routing-strategy")
        {
            var variants = new[] { "always", "sensitive-only", "low-confidence" };
            var variant = variants[hash % variants.Length];
            
            _logger.LogInformation("Experiment {ExperimentName}: Selected variant {Variant} for session {SessionId}",
                experimentName, variant, sessionId);
            
            return variant;
        }

        _logger.LogInformation("Experiment {ExperimentName}: Using default variant {Variant}",
            experimentName, defaultVariant);
        
        return defaultVariant;
    }
}
