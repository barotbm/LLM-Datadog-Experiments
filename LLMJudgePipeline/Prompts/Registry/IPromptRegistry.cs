using LLMJudgePipeline.Prompts.Models;

namespace LLMJudgePipeline.Prompts.Registry;

/// <summary>
/// Interface for prompt registry operations
/// </summary>
public interface IPromptRegistry
{
    /// <summary>
    /// Loads a prompt by name and version synchronously
    /// </summary>
    PromptDefinition Load(string name, string version);

    /// <summary>
    /// Loads a prompt by name and version asynchronously
    /// </summary>
    Task<PromptDefinition> LoadAsync(string name, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to load a prompt, returns null if not found
    /// </summary>
    PromptDefinition? TryLoad(string name, string version);

    /// <summary>
    /// Lists all available versions of a prompt
    /// </summary>
    List<string> ListVersions(string name, string? category = null);

    /// <summary>
    /// Gets the latest version number of a prompt
    /// </summary>
    string GetLatestVersion(string name, string? category = null);

    /// <summary>
    /// Loads the latest version of a prompt
    /// </summary>
    PromptDefinition LoadLatest(string name, string? category = null);

    /// <summary>
    /// Clears the cache for specific prompt or all prompts
    /// </summary>
    void ClearCache(string? name = null, string? version = null);

    /// <summary>
    /// Pre-loads prompts into cache for performance
    /// </summary>
    void Warmup(Dictionary<string, string> promptVersions);
}
