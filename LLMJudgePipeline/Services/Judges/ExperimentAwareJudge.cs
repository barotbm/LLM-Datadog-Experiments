using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Models;

namespace LLMJudgePipeline.Services.Judges;

public class ExperimentAwareJudge : ILlmJudge
{
    private readonly IDatadogExperiments _experiments;
    private readonly IDatadogMetrics _metrics;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExperimentAwareJudge> _logger;

    public ExperimentAwareJudge(
        IDatadogExperiments experiments,
        IDatadogMetrics metrics,
        IServiceProvider serviceProvider,
        ILogger<ExperimentAwareJudge> logger)
    {
        _experiments = experiments;
        _metrics = metrics;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<JudgeResult> EvaluateAsync(string userMessage, GeneratorResult output)
    {
        // Default session ID if not provided
        var sessionId = Guid.NewGuid().ToString();
        
        // Get the variant from Datadog experiments
        var variant = _experiments.GetVariant("llm-judge-model", sessionId);

        _logger.LogInformation("Selected judge variant: {Variant} for session {SessionId}", variant, sessionId);

        // Select the appropriate judge based on variant
        ILlmJudge judge = variant switch
        {
            "gpt4o-mini" => _serviceProvider.GetRequiredService<Gpt4oMiniJudge>(),
            "gpt41-mini" => _serviceProvider.GetRequiredService<Gpt41MiniJudge>(),
            "gpt4o-strong" => _serviceProvider.GetRequiredService<Gpt4oStrongJudge>(),
            _ => _serviceProvider.GetRequiredService<Gpt4oMiniJudge>() // default
        };

        // Evaluate using the selected judge
        var result = await judge.EvaluateAsync(userMessage, output);

        // Track metrics with Datadog
        var tags = new[]
        {
            $"experiment:llm-judge-model",
            $"variant:{variant}",
            $"intent:{output.Intent}",
            $"judge_model:{result.JudgeModel}"
        };

        if (result.IsValid)
        {
            _metrics.Increment("llm.judge.accept", 1, tags);
        }
        else
        {
            _metrics.Increment("llm.judge.reject", 1, tags);
            
            var violationTags = tags.Concat(new[] { $"violation_type:{result.Violation}" }).ToArray();
            _metrics.Increment("llm.judge.violation", 1, violationTags);
        }

        _metrics.Histogram("llm.judge.latency_ms", result.LatencyMs, tags);

        _logger.LogInformation(
            "Judge evaluation complete: Variant={Variant}, Model={Model}, Valid={Valid}, Violation={Violation}, Latency={Latency}ms",
            variant, result.JudgeModel, result.IsValid, result.Violation, result.LatencyMs);

        return result;
    }

    public async Task<JudgeResult> EvaluateWithSessionAsync(string userMessage, GeneratorResult output, string sessionId)
    {
        // Get the variant from Datadog experiments
        var variant = _experiments.GetVariant("llm-judge-model", sessionId);

        _logger.LogInformation("Selected judge variant: {Variant} for session {SessionId}", variant, sessionId);

        // Select the appropriate judge based on variant
        ILlmJudge judge = variant switch
        {
            "gpt4o-mini" => _serviceProvider.GetRequiredService<Gpt4oMiniJudge>(),
            "gpt41-mini" => _serviceProvider.GetRequiredService<Gpt41MiniJudge>(),
            "gpt4o-strong" => _serviceProvider.GetRequiredService<Gpt4oStrongJudge>(),
            _ => _serviceProvider.GetRequiredService<Gpt4oMiniJudge>() // default
        };

        // Evaluate using the selected judge
        var result = await judge.EvaluateAsync(userMessage, output);

        // Track metrics with Datadog
        var tags = new[]
        {
            $"experiment:llm-judge-model",
            $"variant:{variant}",
            $"intent:{output.Intent}",
            $"judge_model:{result.JudgeModel}"
        };

        if (result.IsValid)
        {
            _metrics.Increment("llm.judge.accept", 1, tags);
        }
        else
        {
            _metrics.Increment("llm.judge.reject", 1, tags);
            
            var violationTags = tags.Concat(new[] { $"violation_type:{result.Violation}" }).ToArray();
            _metrics.Increment("llm.judge.violation", 1, violationTags);
        }

        _metrics.Histogram("llm.judge.latency_ms", result.LatencyMs, tags);

        return result;
    }
}
