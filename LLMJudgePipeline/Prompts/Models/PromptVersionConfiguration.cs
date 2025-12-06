namespace LLMJudgePipeline.Prompts.Models;

/// <summary>
/// Configuration for prompt versions used by the application
/// </summary>
public class PromptVersionConfiguration
{
    /// <summary>
    /// Version of the intent classifier prompt to use
    /// </summary>
    public string IntentClassifier { get; set; } = "v1.0.0";

    /// <summary>
    /// Version of the judge evaluation prompt to use
    /// </summary>
    public string JudgeEvaluation { get; set; } = "v1.0.0";

    /// <summary>
    /// Version of the summary generator prompt to use
    /// </summary>
    public string SummaryGenerator { get; set; } = "v1.0.0";

    /// <summary>
    /// Version of the guardrail prompt to use
    /// </summary>
    public string Guardrail { get; set; } = "v1.0.0";
}
