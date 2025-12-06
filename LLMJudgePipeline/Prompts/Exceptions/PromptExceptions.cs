namespace LLMJudgePipeline.Prompts.Exceptions;

/// <summary>
/// Exception thrown when a prompt cannot be found
/// </summary>
public class PromptNotFoundException : Exception
{
    public string PromptName { get; }
    public string Version { get; }

    public PromptNotFoundException(string promptName, string version)
        : base($"Prompt '{promptName}' version '{version}' not found")
    {
        PromptName = promptName;
        Version = version;
    }

    public PromptNotFoundException(string promptName, string version, Exception innerException)
        : base($"Prompt '{promptName}' version '{version}' not found", innerException)
    {
        PromptName = promptName;
        Version = version;
    }
}

/// <summary>
/// Exception thrown when a prompt fails validation
/// </summary>
public class PromptValidationException : Exception
{
    public string PromptName { get; }
    public string Version { get; }
    public List<string> ValidationErrors { get; }

    public PromptValidationException(string promptName, string version, List<string> validationErrors)
        : base($"Prompt '{promptName}' version '{version}' failed validation: {string.Join(", ", validationErrors)}")
    {
        PromptName = promptName;
        Version = version;
        ValidationErrors = validationErrors;
    }
}

/// <summary>
/// Exception thrown when a prompt has an invalid version format
/// </summary>
public class InvalidPromptVersionException : Exception
{
    public string Version { get; }

    public InvalidPromptVersionException(string version)
        : base($"Invalid semantic version format: '{version}'. Expected format: vX.Y.Z")
    {
        Version = version;
    }
}

/// <summary>
/// Exception thrown when a prompt file has invalid JSON or schema
/// </summary>
public class PromptSchemaException : Exception
{
    public string PromptName { get; }
    public string FilePath { get; }

    public PromptSchemaException(string promptName, string filePath, string message)
        : base($"Schema error in prompt '{promptName}' at '{filePath}': {message}")
    {
        PromptName = promptName;
        FilePath = filePath;
    }

    public PromptSchemaException(string promptName, string filePath, string message, Exception innerException)
        : base($"Schema error in prompt '{promptName}' at '{filePath}': {message}", innerException)
    {
        PromptName = promptName;
        FilePath = filePath;
    }
}
