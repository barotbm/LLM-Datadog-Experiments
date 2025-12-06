namespace LLMJudgePipeline.Models;

public class GeneratorResult
{
    public string Intent { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public Dictionary<string, object>? Entities { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TokensUsed { get; set; }
    public double LatencyMs { get; set; }
}
