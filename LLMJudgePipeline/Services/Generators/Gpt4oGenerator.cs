using Azure;
using Azure.AI.OpenAI;
using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Models;
using LLMJudgePipeline.Prompts.Models;
using LLMJudgePipeline.Prompts.Registry;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;

namespace LLMJudgePipeline.Services.Generators;

public class Gpt4oGenerator : ILlmGenerator
{
    private readonly OpenAIClient _openAiClient;
    private readonly ILogger<Gpt4oGenerator> _logger;
    private readonly IPromptRegistry _promptRegistry;
    private readonly PromptVersionConfiguration _promptConfig;
    private PromptDefinition? _cachedPrompt;

    public Gpt4oGenerator(
        OpenAIClient openAiClient, 
        ILogger<Gpt4oGenerator> logger,
        IPromptRegistry promptRegistry,
        IOptions<PromptVersionConfiguration> promptConfig)
    {
        _openAiClient = openAiClient;
        _logger = logger;
        _promptRegistry = promptRegistry;
        _promptConfig = promptConfig.Value;
    }

    public async Task<GeneratorResult> GenerateAsync(string userMessage, string? systemPrompt = null)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            // Load prompt from registry if not using custom prompt
            var prompt = systemPrompt == null ? GetPrompt() : null;
            var finalSystemMessage = systemPrompt ?? prompt?.SystemMessage ?? throw new InvalidOperationException("No system prompt available");
            
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = prompt?.Model ?? "gpt-4o",
                Messages =
                {
                    new ChatRequestSystemMessage(finalSystemMessage),
                    new ChatRequestUserMessage(userMessage)
                },
                Temperature = prompt?.Temperature ?? 0.3f,
                MaxTokens = prompt?.MaxTokens ?? 500,
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

            _logger.LogInformation("Generator completed: Intent={Intent}, Confidence={Confidence}, Latency={Latency}ms, PromptVersion={PromptVersion}",
                result.Intent, result.Confidence, result.LatencyMs, prompt?.Version ?? "custom");

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

    private PromptDefinition GetPrompt()
    {
        if (_cachedPrompt == null)
        {
            _cachedPrompt = _promptRegistry.Load("classify", _promptConfig.IntentClassifier);
            _logger.LogInformation("Loaded intent classifier prompt: version={Version}, model={Model}",
                _cachedPrompt.Version, _cachedPrompt.Model);
        }
        return _cachedPrompt;
    }

    private class GeneratorJsonSchema
    {
        public string intent { get; set; } = string.Empty;
        public double confidence { get; set; }
        public Dictionary<string, object>? entities { get; set; }
    }
}
