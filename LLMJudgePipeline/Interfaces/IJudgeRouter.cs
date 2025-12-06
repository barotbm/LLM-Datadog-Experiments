using LLMJudgePipeline.Models;

namespace LLMJudgePipeline.Interfaces;

public interface IJudgeRouter
{
    Task<JudgeResult> RouteAsync(string userMessage, GeneratorResult output, string sessionId);
    bool ShouldEvaluate(GeneratorResult output);
}
