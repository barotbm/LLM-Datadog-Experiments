using LLMJudgePipeline.Models;

namespace LLMJudgePipeline.Interfaces;

public interface ILlmJudge
{
    Task<JudgeResult> EvaluateAsync(string userMessage, GeneratorResult output);
}
