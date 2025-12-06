using Azure;
using Azure.AI.OpenAI;
using LLMJudgePipeline.Interfaces;

namespace LLMJudgePipeline.Services.Embedding;

public class EmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient _openAiClient;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(OpenAIClient openAiClient, ILogger<EmbeddingService> logger)
    {
        _openAiClient = openAiClient;
        _logger = logger;
    }

    public async Task<double> CalculateCosineSimilarityAsync(string text1, string text2)
    {
        try
        {
            var embedding1 = await GetEmbeddingAsync(text1);
            var embedding2 = await GetEmbeddingAsync(text2);

            return CosineSimilarity(embedding1, embedding2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cosine similarity");
            return 0.0;
        }
    }

    public async Task<double[]> GetEmbeddingAsync(string text)
    {
        try
        {
            var embeddingsOptions = new EmbeddingsOptions("text-embedding-ada-002", new[] { text });
            var response = await _openAiClient.GetEmbeddingsAsync(embeddingsOptions);
            
            var floatArray = response.Value.Data[0].Embedding.ToArray();
            return Array.ConvertAll(floatArray, x => (double)x);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embedding for text: {Text}", text);
            throw;
        }
    }

    private double CosineSimilarity(double[] vector1, double[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        double dotProduct = 0.0;
        double magnitude1 = 0.0;
        double magnitude2 = 0.0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
        {
            return 0.0;
        }

        return dotProduct / (magnitude1 * magnitude2);
    }
}
