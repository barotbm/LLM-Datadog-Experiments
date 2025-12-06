namespace LLMJudgePipeline.Models;

public class DriftEvalResult
{
    public bool DriftDetected { get; set; }
    public double Similarity { get; set; }
    public bool DomainMismatch { get; set; }
    public bool InvalidStateTransition { get; set; }
    public string? Details { get; set; }
}
