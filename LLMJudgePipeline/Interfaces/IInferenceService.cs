using LLMJudgePipeline.Models;

namespace LLMJudgePipeline.Interfaces;

public interface IInferenceService
{
    Task<InferenceResponse> ProcessAsync(InferenceRequest request);
}
