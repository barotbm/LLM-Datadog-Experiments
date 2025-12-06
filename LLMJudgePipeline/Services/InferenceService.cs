using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Models;
using System.Collections.Concurrent;

namespace LLMJudgePipeline.Services;

public class InferenceService : IInferenceService
{
    private readonly ILlmGenerator _generator;
    private readonly IDriftEvaluator _driftEvaluator;
    private readonly IJudgeRouter _judgeRouter;
    private readonly IDatadogMetrics _metrics;
    private readonly IDatadogExperiments _experiments;
    private readonly ILogger<InferenceService> _logger;

    // Simple in-memory store for conversation history (in production, use Redis or similar)
    private static readonly ConcurrentDictionary<string, List<string>> ConversationHistory = new();

    public InferenceService(
        ILlmGenerator generator,
        IDriftEvaluator driftEvaluator,
        IJudgeRouter judgeRouter,
        IDatadogMetrics metrics,
        IDatadogExperiments experiments,
        ILogger<InferenceService> logger)
    {
        _generator = generator;
        _driftEvaluator = driftEvaluator;
        _judgeRouter = judgeRouter;
        _metrics = metrics;
        _experiments = experiments;
        _logger = logger;
    }

    public async Task<InferenceResponse> ProcessAsync(InferenceRequest request)
    {
        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        
        _logger.LogInformation("Processing inference request for session {SessionId}: {Message}", 
            sessionId, request.Message);

        try
        {
            // Step 1: Call GPT-4o generator
            _logger.LogInformation("Step 1: Calling LLM generator");
            var generatorResult = await _generator.GenerateAsync(request.Message);

            _metrics.Increment("llm.generator.invoked", 1, new[]
            {
                $"intent:{generatorResult.Intent}",
                $"confidence_bucket:{GetConfidenceBucket(generatorResult.Confidence)}"
            });

            _metrics.Histogram("llm.generator.latency_ms", generatorResult.LatencyMs, new[]
            {
                $"intent:{generatorResult.Intent}"
            });

            // Step 2: Run drift evaluation
            _logger.LogInformation("Step 2: Running drift evaluation");
            var recentIntents = GetRecentIntents(sessionId);
            var driftResult = await _driftEvaluator.EvaluateAsync(
                request.Message, 
                generatorResult, 
                recentIntents);

            _metrics.Increment("llm.drift.evaluated", 1, new[]
            {
                $"drift_detected:{driftResult.DriftDetected}",
                $"intent:{generatorResult.Intent}"
            });

            if (driftResult.DriftDetected)
            {
                _logger.LogWarning("Drift detected: {Details}", driftResult.Details);
            }

            // Step 3: Use risk-based routing to decide if judge is needed
            _logger.LogInformation("Step 3: Routing to judge (if needed)");
            var judgeResult = await _judgeRouter.RouteAsync(request.Message, generatorResult, sessionId);

            // Get the variant used for the judge
            var judgeVariant = judgeResult.WasSkipped 
                ? "skipped" 
                : _experiments.GetVariant("llm-judge-model", sessionId);

            var routingStrategy = _experiments.GetVariant("judge-routing-strategy", sessionId);

            // Step 4: Handle judge rejection with fallback logic
            string finalIntent = generatorResult.Intent;
            bool finalValid = judgeResult.IsValid;

            if (!judgeResult.IsValid && !judgeResult.WasSkipped)
            {
                _logger.LogWarning("Judge rejected the output: {Violation}. Triggering fallback.",
                    judgeResult.Violation);

                // Fallback to NLP or human review
                // For now, we'll mark it as requiring review
                finalIntent = "RequiresReview";
                finalValid = false;

                _metrics.Increment("llm.fallback.triggered", 1, new[]
                {
                    $"violation:{judgeResult.Violation}",
                    $"original_intent:{generatorResult.Intent}"
                });
            }

            // Step 5: Update conversation history
            UpdateConversationHistory(sessionId, finalIntent);

            // Step 6: Log structured data to Datadog
            var tags = new[]
            {
                $"experiment:llm-judge-model",
                $"variant:{judgeVariant}",
                $"routing_strategy:{routingStrategy}",
                $"intent:{finalIntent}",
                $"drift_detected:{driftResult.DriftDetected}",
                $"judge_valid:{finalValid}",
                $"judge_skipped:{judgeResult.WasSkipped}"
            };

            _metrics.Increment("llm.inference.completed", 1, tags);

            _logger.LogInformation(
                "Inference complete: Intent={Intent}, Confidence={Confidence}, JudgeValid={JudgeValid}, DriftDetected={DriftDetected}, Variant={Variant}, Strategy={Strategy}",
                finalIntent, generatorResult.Confidence, finalValid, driftResult.DriftDetected, judgeVariant, routingStrategy);

            // Step 7: Build and return response
            return new InferenceResponse
            {
                Intent = finalIntent,
                Confidence = generatorResult.Confidence,
                JudgeValid = finalValid,
                DriftDetected = driftResult.DriftDetected,
                VariantUsed = judgeVariant,
                JudgeSkipped = judgeResult.WasSkipped,
                RoutingStrategy = routingStrategy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inference request");
            
            _metrics.Increment("llm.inference.error", 1, new[]
            {
                $"error_type:{ex.GetType().Name}"
            });

            throw;
        }
    }

    private List<string> GetRecentIntents(string sessionId)
    {
        if (ConversationHistory.TryGetValue(sessionId, out var history))
        {
            return history.TakeLast(3).ToList();
        }
        return new List<string>();
    }

    private void UpdateConversationHistory(string sessionId, string intent)
    {
        ConversationHistory.AddOrUpdate(
            sessionId,
            new List<string> { intent },
            (key, existingList) =>
            {
                existingList.Add(intent);
                // Keep only last 10 intents
                if (existingList.Count > 10)
                {
                    existingList.RemoveAt(0);
                }
                return existingList;
            });
    }

    private string GetConfidenceBucket(double confidence)
    {
        return confidence switch
        {
            >= 0.9 => "high",
            >= 0.75 => "medium",
            >= 0.5 => "low",
            _ => "very_low"
        };
    }
}
