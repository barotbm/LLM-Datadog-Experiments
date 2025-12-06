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

public class Gpt4oStrongJudge : ILlmJudge
{
    private readonly OpenAIClient _openAiClient;
    private readonly ILogger<Gpt4oStrongJudge> _logger;
    private readonly IPromptRegistry _promptRegistry;
    private readonly PromptVersionConfiguration _promptVersions;
    private PromptDefinition? _cachedPrompt;

    public Gpt4oStrongJudge(
        OpenAIClient openAiClient, 
        ILogger<Gpt4oStrongJudge> logger,
        IPromptRegistry promptRegistry,
        IOptions<PromptVersionConfiguration> promptVersions)
    {
        _openAiClient = openAiClient;
        _logger = logger;
        _promptRegistry = promptRegistry;
        _promptVersions = promptVersions.Value;
    }

    private PromptDefinition GetPrompt()
    {
        if (_cachedPrompt == null)
        {
            var version = _promptVersions.JudgeEvaluation ?? "v1.0.0";
            _cachedPrompt = _promptRegistry.Load("judge-evaluation", version);
            _logger.LogInformation("Loaded judge-evaluation prompt version {Version}", version);
        }
        return _cachedPrompt;
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
                DeploymentName = "gpt-4o",
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
                JudgeModel = "gpt-4o-strong",
                LatencyMs = sw.ElapsedMilliseconds
            };

            _logger.LogInformation("Judge evaluation: Model={Model}, Valid={Valid}, Violation={Violation}, PromptVersion={PromptVersion}, Latency={Latency}ms",
                result.JudgeModel, result.IsValid, result.Violation, prompt.Version, result.LatencyMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Gpt4oStrongJudge");
            sw.Stop();

            return new JudgeResult
            {
                IsValid = false,
                Violation = "error",
                Confidence = 0.0,
                JudgeModel = "gpt-4o-strong",
                LatencyMs = sw.ElapsedMilliseconds,
                Reason = ex.Message
            };
        }
    }
}
