using LLMJudgePipeline.Prompts.Exceptions;
using LLMJudgePipeline.Prompts.Models;
using System.Text.RegularExpressions;

namespace LLMJudgePipeline.Prompts.Validation;

/// <summary>
/// Validates prompt definitions for correctness and completeness
/// </summary>
public class PromptSchemaValidator
{
    private static readonly Regex SemanticVersionRegex = new(@"^v?\d+\.\d+\.\d+$", RegexOptions.Compiled);
    private readonly ILogger<PromptSchemaValidator> _logger;

    public PromptSchemaValidator(ILogger<PromptSchemaValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a prompt definition and returns validation errors
    /// </summary>
    public List<string> Validate(PromptDefinition prompt, string filePath)
    {
        var errors = new List<string>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(prompt.Version))
        {
            errors.Add("Version is required");
        }
        else if (!IsValidSemanticVersion(prompt.Version))
        {
            errors.Add($"Version '{prompt.Version}' is not a valid semantic version (expected format: vX.Y.Z or X.Y.Z)");
        }

        if (string.IsNullOrWhiteSpace(prompt.Name))
        {
            errors.Add("Name is required");
        }

        if (string.IsNullOrWhiteSpace(prompt.Model))
        {
            errors.Add("Model is required");
        }

        if (string.IsNullOrWhiteSpace(prompt.Schema))
        {
            errors.Add("Schema is required");
        }

        if (string.IsNullOrWhiteSpace(prompt.SystemMessage))
        {
            errors.Add("SystemMessage is required");
        }

        // Validate schema-specific requirements
        ValidateSchemaSpecificRequirements(prompt, errors);

        // Validate metadata structure
        ValidateMetadata(prompt, errors);

        // Validate examples if present
        if (prompt.Examples != null)
        {
            ValidateExamples(prompt.Examples, errors);
        }

        // Validate temperature range
        if (prompt.Temperature.HasValue && (prompt.Temperature.Value < 0 || prompt.Temperature.Value > 2))
        {
            errors.Add($"Temperature must be between 0 and 2, got {prompt.Temperature.Value}");
        }

        // Validate max tokens
        if (prompt.MaxTokens.HasValue && prompt.MaxTokens.Value <= 0)
        {
            errors.Add($"MaxTokens must be positive, got {prompt.MaxTokens.Value}");
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Prompt validation failed for {FilePath}: {Errors}", 
                filePath, string.Join(", ", errors));
        }

        return errors;
    }

    /// <summary>
    /// Validates and throws exception if prompt is invalid
    /// </summary>
    public void ValidateAndThrow(PromptDefinition prompt, string filePath)
    {
        var errors = Validate(prompt, filePath);
        if (errors.Count > 0)
        {
            throw new PromptValidationException(prompt.Name ?? "unknown", prompt.Version ?? "unknown", errors);
        }
    }

    /// <summary>
    /// Checks if a version string follows semantic versioning
    /// </summary>
    public bool IsValidSemanticVersion(string version)
    {
        return SemanticVersionRegex.IsMatch(version);
    }

    private void ValidateSchemaSpecificRequirements(PromptDefinition prompt, List<string> errors)
    {
        switch (prompt.Schema?.ToLowerInvariant())
        {
            case "intent-classifier":
                if (prompt.EnumIntents == null || prompt.EnumIntents.Count == 0)
                {
                    errors.Add("Intent-classifier schema requires EnumIntents to be defined");
                }
                break;

            case "judge":
                // Judge prompts should have specific metadata
                if (!prompt.Metadata.ContainsKey("evaluationCriteria"))
                {
                    errors.Add("Judge schema should include 'evaluationCriteria' in metadata");
                }
                break;

            case "summarizer":
                // Summarizer prompts should specify max length
                if (!prompt.MaxTokens.HasValue)
                {
                    errors.Add("Summarizer schema should specify MaxTokens");
                }
                break;
        }
    }

    private void ValidateMetadata(PromptDefinition prompt, List<string> errors)
    {
        if (prompt.Metadata == null)
        {
            return;
        }

        // Validate metadata keys are not empty
        foreach (var key in prompt.Metadata.Keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add("Metadata keys cannot be empty");
                break;
            }
        }
    }

    private void ValidateExamples(List<PromptExample> examples, List<string> errors)
    {
        for (int i = 0; i < examples.Count; i++)
        {
            var example = examples[i];
            if (string.IsNullOrWhiteSpace(example.Input))
            {
                errors.Add($"Example {i + 1}: Input is required");
            }
            if (string.IsNullOrWhiteSpace(example.Output))
            {
                errors.Add($"Example {i + 1}: Output is required");
            }
        }
    }

    /// <summary>
    /// Normalizes version string to ensure consistent format (vX.Y.Z)
    /// </summary>
    public string NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidPromptVersionException(version);
        }

        // Remove 'v' prefix if present, then add it back
        var normalized = version.TrimStart('v', 'V');
        
        if (!SemanticVersionRegex.IsMatch(normalized))
        {
            throw new InvalidPromptVersionException(version);
        }

        return $"v{normalized}";
    }
}
