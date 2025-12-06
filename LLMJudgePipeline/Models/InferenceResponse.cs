namespace LLMJudgePipeline.Models;

public class InferenceResponse
{
    public string Intent { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool JudgeValid { get; set; }
    public bool DriftDetected { get; set; }
    public string? VariantUsed { get; set; }
    public bool JudgeSkipped { get; set; }
    public string? RoutingStrategy { get; set; }
}
