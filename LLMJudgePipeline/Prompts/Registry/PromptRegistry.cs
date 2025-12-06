using LLMJudgePipeline.Prompts.Exceptions;
using LLMJudgePipeline.Prompts.Models;
using LLMJudgePipeline.Prompts.Validation;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LLMJudgePipeline.Prompts.Registry;

/// <summary>
/// Central registry for loading, caching, and managing versioned prompts.
/// Prompts are loaded from JSON files and validated before use.
/// </summary>
public class PromptRegistry : IPromptRegistry
{
    private readonly ILogger<PromptRegistry> _logger;
    private readonly PromptSchemaValidator _validator;
    private readonly string _promptsBasePath;
    private readonly ConcurrentDictionary<string, PromptDefinition> _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public PromptRegistry(
        ILogger<PromptRegistry> logger,
        PromptSchemaValidator validator,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _validator = validator;
        _promptsBasePath = Path.Combine(environment.ContentRootPath, "Prompts");
        _cache = new ConcurrentDictionary<string, PromptDefinition>();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        _logger.LogInformation("PromptRegistry initialized with base path: {BasePath}", _promptsBasePath);
    }

    /// <summary>
    /// Loads a prompt by name and version. Results are cached.
    /// </summary>
    public PromptDefinition Load(string name, string version)
    {
        var normalizedVersion = _validator.NormalizeVersion(version);
        var cacheKey = GetCacheKey(name, normalizedVersion);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cachedPrompt))
        {
            _logger.LogDebug("Prompt loaded from cache: {Name}, {Version}", name, normalizedVersion);
            return cachedPrompt;
        }

        // Load from disk
        var prompt = LoadFromDisk(name, normalizedVersion);

        // Cache it
        _cache[cacheKey] = prompt;

        _logger.LogInformation(
            "PromptLoaded {Name}, {Version}, {Model}, Schema={Schema}",
            prompt.Name, prompt.Version, prompt.Model, prompt.Schema);

        return prompt;
    }

    /// <summary>
    /// Loads a prompt by name and version asynchronously
    /// </summary>
    public async Task<PromptDefinition> LoadAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        var normalizedVersion = _validator.NormalizeVersion(version);
        var cacheKey = GetCacheKey(name, normalizedVersion);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cachedPrompt))
        {
            _logger.LogDebug("Prompt loaded from cache: {Name}, {Version}", name, normalizedVersion);
            return cachedPrompt;
        }

        // Load from disk asynchronously
        var prompt = await LoadFromDiskAsync(name, normalizedVersion, cancellationToken);

        // Cache it
        _cache[cacheKey] = prompt;

        _logger.LogInformation(
            "PromptLoaded {Name}, {Version}, {Model}, Schema={Schema}",
            prompt.Name, prompt.Version, prompt.Model, prompt.Schema);

        return prompt;
    }

    /// <summary>
    /// Attempts to load a prompt, returns null if not found
    /// </summary>
    public PromptDefinition? TryLoad(string name, string version)
    {
        try
        {
            return Load(name, version);
        }
        catch (PromptNotFoundException)
        {
            _logger.LogWarning("Prompt not found: {Name}, {Version}", name, version);
            return null;
        }
    }

    /// <summary>
    /// Lists all available versions of a prompt
    /// </summary>
    public List<string> ListVersions(string name, string? category = null)
    {
        var searchPath = category != null 
            ? Path.Combine(_promptsBasePath, category)
            : _promptsBasePath;

        if (!Directory.Exists(searchPath))
        {
            return new List<string>();
        }

        var pattern = $"{name}-v*.prompt.json";
        var files = Directory.GetFiles(searchPath, pattern, SearchOption.AllDirectories);

        var versions = files
            .Select(ExtractVersionFromFilePath)
            .Where(v => v != null)
            .Cast<string>()
            .OrderByDescending(v => v)
            .ToList();

        return versions;
    }

    /// <summary>
    /// Gets the latest version of a prompt
    /// </summary>
    public string GetLatestVersion(string name, string? category = null)
    {
        var versions = ListVersions(name, category);
        
        if (versions.Count == 0)
        {
            throw new PromptNotFoundException(name, "latest");
        }

        return versions.First();
    }

    /// <summary>
    /// Loads the latest version of a prompt
    /// </summary>
    public PromptDefinition LoadLatest(string name, string? category = null)
    {
        var latestVersion = GetLatestVersion(name, category);
        return Load(name, latestVersion);
    }

    /// <summary>
    /// Clears the cache for a specific prompt or all prompts
    /// </summary>
    public void ClearCache(string? name = null, string? version = null)
    {
        if (name == null)
        {
            _cache.Clear();
            _logger.LogInformation("Prompt cache cleared completely");
        }
        else if (version == null)
        {
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith($"{name}:")).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
            _logger.LogInformation("Prompt cache cleared for: {Name}", name);
        }
        else
        {
            var normalizedVersion = _validator.NormalizeVersion(version);
            var cacheKey = GetCacheKey(name, normalizedVersion);
            _cache.TryRemove(cacheKey, out _);
            _logger.LogInformation("Prompt cache cleared for: {Name}, {Version}", name, normalizedVersion);
        }
    }

    /// <summary>
    /// Pre-loads prompts into cache
    /// </summary>
    public void Warmup(Dictionary<string, string> promptVersions)
    {
        _logger.LogInformation("Warming up prompt cache with {Count} prompts", promptVersions.Count);

        foreach (var kvp in promptVersions)
        {
            try
            {
                Load(kvp.Key, kvp.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to warmup prompt: {Name}, {Version}", kvp.Key, kvp.Value);
            }
        }
    }

    private PromptDefinition LoadFromDisk(string name, string version)
    {
        var filePath = FindPromptFile(name, version);

        if (!File.Exists(filePath))
        {
            throw new PromptNotFoundException(name, version);
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var prompt = JsonSerializer.Deserialize<PromptDefinition>(json, _jsonOptions);

            if (prompt == null)
            {
                throw new PromptSchemaException(name, filePath, "Failed to deserialize prompt JSON");
            }

            // Validate the prompt
            _validator.ValidateAndThrow(prompt, filePath);

            // Verify version matches
            var normalizedPromptVersion = _validator.NormalizeVersion(prompt.Version);
            if (normalizedPromptVersion != version)
            {
                throw new PromptValidationException(
                    name, 
                    version, 
                    new List<string> { $"Version mismatch: file contains {normalizedPromptVersion} but requested {version}" });
            }

            return prompt;
        }
        catch (JsonException ex)
        {
            throw new PromptSchemaException(name, filePath, "Invalid JSON format", ex);
        }
    }

    private async Task<PromptDefinition> LoadFromDiskAsync(string name, string version, CancellationToken cancellationToken)
    {
        var filePath = FindPromptFile(name, version);

        if (!File.Exists(filePath))
        {
            throw new PromptNotFoundException(name, version);
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var prompt = JsonSerializer.Deserialize<PromptDefinition>(json, _jsonOptions);

            if (prompt == null)
            {
                throw new PromptSchemaException(name, filePath, "Failed to deserialize prompt JSON");
            }

            // Validate the prompt
            _validator.ValidateAndThrow(prompt, filePath);

            // Verify version matches
            var normalizedPromptVersion = _validator.NormalizeVersion(prompt.Version);
            if (normalizedPromptVersion != version)
            {
                throw new PromptValidationException(
                    name, 
                    version, 
                    new List<string> { $"Version mismatch: file contains {normalizedPromptVersion} but requested {version}" });
            }

            return prompt;
        }
        catch (JsonException ex)
        {
            throw new PromptSchemaException(name, filePath, "Invalid JSON format", ex);
        }
    }

    private string FindPromptFile(string name, string version)
    {
        // Try different category folders
        var categories = new[] { "Intents", "Summaries", "Guardrails", "Redaction", "" };
        var fileName = $"{name}-{version}.prompt.json";

        foreach (var category in categories)
        {
            var filePath = string.IsNullOrEmpty(category)
                ? Path.Combine(_promptsBasePath, fileName)
                : Path.Combine(_promptsBasePath, category, fileName);

            if (File.Exists(filePath))
            {
                return filePath;
            }
        }

        // If not found, return the default path for error messaging
        return Path.Combine(_promptsBasePath, fileName);
    }

    private string? ExtractVersionFromFilePath(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        // Format: name-vX.Y.Z.prompt
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"-v(\d+\.\d+\.\d+)\.prompt$");
        return match.Success ? $"v{match.Groups[1].Value}" : null;
    }

    private string GetCacheKey(string name, string version)
    {
        return $"{name}:{version}";
    }
}
