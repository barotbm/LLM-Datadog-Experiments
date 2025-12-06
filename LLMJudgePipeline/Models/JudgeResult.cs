namespace LLMJudgePipeline.Models;

public class JudgeResult
{
    public bool IsValid { get; set; }
    public string Violation { get; set; } = "none";
    public double Confidence { get; set; }
    public string JudgeModel { get; set; } = string.Empty;
    public double LatencyMs { get; set; }
    public bool WasSkipped { get; set; } = false;
    public string? Reason { get; set; }
}

public class JudgeEvaluationSchema
{
    public bool is_valid { get; set; }
    public string violation { get; set; } = "none";
    public double confidence { get; set; }
}
