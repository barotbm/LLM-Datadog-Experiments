using Azure;
using Azure.AI.OpenAI;
using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Models;
using Newtonsoft.Json;
using System.Diagnostics;

namespace LLMJudgePipeline.Services.Generators;

public class Gpt4oGenerator : ILlmGenerator
{
    private readonly OpenAIClient _openAiClient;
    private readonly ILogger<Gpt4oGenerator> _logger;
    private const string DefaultSystemPrompt = @"You are an intent classification system for a loan servicing platform.
Analyze the user's message and return a JSON response with the following structure:
{
  ""intent"": ""<intent_name>"",
  ""confidence"": <0.0-1.0>,
  ""entities"": {
    ""key"": ""value""
  }
}

Valid intents:
- PayoffQuote: User wants a loan payoff amount
- Payment: User wants to make a payment
- AddressChange: User wants to update their address
- BalanceInquiry: User asks about their balance
- RateInformation: User asks about interest rates
- GeneralInquiry: General questions
- Other: Anything else

Extract relevant entities like amounts, dates, addresses, etc.";

    public Gpt4oGenerator(OpenAIClient openAiClient, ILogger<Gpt4oGenerator> logger)
    {
        _openAiClient = openAiClient;
        _logger = logger;
    }

    public async Task<GeneratorResult> GenerateAsync(string userMessage, string? systemPrompt = null)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = "gpt-4o",
                Messages =
                {
                    new ChatRequestSystemMessage(systemPrompt ?? DefaultSystemPrompt),
                    new ChatRequestUserMessage(userMessage)
                },
                Temperature = 0.3f,
                MaxTokens = 500,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            var response = await _openAiClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var completion = response.Value;
            var content = completion.Choices[0].Message.Content;

            sw.Stop();

            // Parse the JSON response
            var parsed = JsonConvert.DeserializeObject<GeneratorJsonSchema>(content);

            var result = new GeneratorResult
            {
                Intent = parsed?.intent ?? "Unknown",
                Confidence = parsed?.confidence ?? 0.0,
                RawJson = content,
                Entities = parsed?.entities,
                Timestamp = DateTime.UtcNow,
                TokensUsed = completion.Usage.TotalTokens,
                LatencyMs = sw.ElapsedMilliseconds
            };

            _logger.LogInformation("Generator completed: Intent={Intent}, Confidence={Confidence}, Latency={Latency}ms",
                result.Intent, result.Confidence, result.LatencyMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Gpt4oGenerator");
            sw.Stop();
            
            return new GeneratorResult
            {
                Intent = "Error",
                Confidence = 0.0,
                RawJson = $"{{\"error\": \"{ex.Message}\"}}",
                LatencyMs = sw.ElapsedMilliseconds
            };
        }
    }

    private class GeneratorJsonSchema
    {
        public string intent { get; set; } = string.Empty;
        public double confidence { get; set; }
        public Dictionary<string, object>? entities { get; set; }
    }
}
