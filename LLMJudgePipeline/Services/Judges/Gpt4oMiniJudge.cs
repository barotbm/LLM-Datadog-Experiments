using Azure;
using Azure.AI.OpenAI;
using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Models;
using LLMJudgePipeline.Prompts.Models;
using LLMJudgePipeline.Prompts.Registry;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;

namespace LLMJudgePipeline.Services.Judges;

public class Gpt4oMiniJudge : ILlmJudge
{
    private readonly OpenAIClient _openAiClient;
    private readonly ILogger<Gpt4oMiniJudge> _logger;
    private readonly IPromptRegistry _promptRegistry;
    private readonly PromptVersionConfiguration _promptConfig;
    private PromptDefinition? _cachedPrompt;

    public Gpt4oMiniJudge(
        OpenAIClient openAiClient, 
        ILogger<Gpt4oMiniJudge> logger,
        IPromptRegistry promptRegistry,
        IOptions<PromptVersionConfiguration> promptConfig)
    {
        _openAiClient = openAiClient;
        _logger = logger;
        _promptRegistry = promptRegistry;
        _promptConfig = promptConfig.Value;
    }

    public async Task<JudgeResult> EvaluateAsync(string userMessage, GeneratorResult output)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var prompt = GetPrompt();
            
            var evaluationPrompt = $@"User Message: ""{userMessage}""
Predicted Intent: ""{output.Intent}""
Confidence: {output.Confidence}

Evaluate this prediction.";

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = "gpt-4o-mini",
                Messages =
                {
                    new ChatRequestSystemMessage(prompt.SystemMessage),
                    new ChatRequestUserMessage(evaluationPrompt)
                },
                Temperature = prompt.Temperature ?? 0.1f,
                MaxTokens = prompt.MaxTokens ?? 200,
                ResponseFormat = ChatCompletionsResponseFormat.JsonObject
            };

            var response = await _openAiClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var content = response.Value.Choices[0].Message.Content;

            sw.Stop();

            var parsed = JsonConvert.DeserializeObject<JudgeEvaluationSchema>(content);

            var result = new JudgeResult
            {
                IsValid = parsed?.is_valid ?? false,
                Violation = parsed?.violation ?? "unknown",
                Confidence = parsed?.confidence ?? 0.0,
                JudgeModel = "gpt-4o-mini",
                LatencyMs = sw.ElapsedMilliseconds
            };

            _logger.LogInformation("Judge evaluation: Model={Model}, Valid={Valid}, Violation={Violation}, PromptVersion={PromptVersion}, Latency={Latency}ms",
                result.JudgeModel, result.IsValid, result.Violation, prompt.Version, result.LatencyMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Gpt4oMiniJudge");
            sw.Stop();

            return new JudgeResult
            {
                IsValid = false,
                Violation = "error",
                Confidence = 0.0,
                JudgeModel = "gpt-4o-mini",
                LatencyMs = sw.ElapsedMilliseconds,
                Reason = ex.Message
            };
        }
    }

    private PromptDefinition GetPrompt()
    {
        if (_cachedPrompt == null)
        {
            _cachedPrompt = _promptRegistry.Load("judge-evaluation", _promptConfig.JudgeEvaluation);
            _logger.LogInformation("Loaded judge evaluation prompt: version={Version}, model={Model}",
                _cachedPrompt.Version, _cachedPrompt.Model);
        }
        return _cachedPrompt;
    }
}
