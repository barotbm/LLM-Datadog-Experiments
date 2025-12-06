using System.Text.Json.Serialization;

namespace LLMJudgePipeline.Prompts.Models;

/// <summary>
/// Represents a versioned prompt definition loaded from JSON files.
/// Prompts are first-class version-controlled assets with semantic versioning.
/// </summary>
public class PromptDefinition
{
    /// <summary>
    /// Semantic version of the prompt (e.g., "1.0.0", "2.1.3")
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Name/identifier of the prompt (e.g., "classify", "summary", "guardrail")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Target LLM model (e.g., "gpt-4o", "gpt-4o-mini", "gpt-4.1-mini")
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Schema type for validation (e.g., "intent-classifier", "judge", "summarizer")
    /// </summary>
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// The system message/prompt sent to the LLM
    /// </summary>
    [JsonPropertyName("systemMessage")]
    public string SystemMessage { get; set; } = string.Empty;

    /// <summary>
    /// List of instructions or guidelines for the LLM
    /// </summary>
    [JsonPropertyName("instructions")]
    public List<string> Instructions { get; set; } = new();

    /// <summary>
    /// Additional metadata for tracking, observability, and context
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Optional: Enumerated list of valid intents for classification prompts
    /// </summary>
    [JsonPropertyName("enumIntents")]
    public List<string>? EnumIntents { get; set; }

    /// <summary>
    /// Optional: Temperature parameter for LLM calls
    /// </summary>
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    /// <summary>
    /// Optional: Max tokens for LLM response
    /// </summary>
    [JsonPropertyName("maxTokens")]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Optional: Examples for few-shot learning
    /// </summary>
    [JsonPropertyName("examples")]
    public List<PromptExample>? Examples { get; set; }

    /// <summary>
    /// When this prompt was created
    /// </summary>
    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// Author or team responsible for this prompt version
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// Change description for this version
    /// </summary>
    [JsonPropertyName("changelog")]
    public string? Changelog { get; set; }
}

/// <summary>
/// Represents an example for few-shot learning in prompts
/// </summary>
public class PromptExample
{
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}
