namespace LLMJudgePipeline.Models;

public class InferenceRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public List<string>? ConversationHistory { get; set; }
}
