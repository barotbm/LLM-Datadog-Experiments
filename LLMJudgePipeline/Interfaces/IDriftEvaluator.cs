using LLMJudgePipeline.Models;

namespace LLMJudgePipeline.Interfaces;

public interface IDriftEvaluator
{
    Task<DriftEvalResult> EvaluateAsync(string userMessage, GeneratorResult output, List<string>? recentIntents = null);
}
