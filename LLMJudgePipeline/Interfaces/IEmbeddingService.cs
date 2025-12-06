using LLMJudgePipeline.Models;

namespace LLMJudgePipeline.Interfaces;

public interface IEmbeddingService
{
    Task<double> CalculateCosineSimilarityAsync(string text1, string text2);
    Task<double[]> GetEmbeddingAsync(string text);
}
