using LLMJudgePipeline.Models;

namespace LLMJudgePipeline.Interfaces;

public interface ILlmGenerator
{
    Task<GeneratorResult> GenerateAsync(string userMessage, string? systemPrompt = null);
}
