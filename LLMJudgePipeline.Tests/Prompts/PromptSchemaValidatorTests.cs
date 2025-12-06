using LLMJudgePipeline.Prompts.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using LLMJudgePipeline.Prompts.Models;
using LLMJudgePipeline.Prompts.Exceptions;

namespace LLMJudgePipeline.Tests.Prompts;

public class PromptSchemaValidatorTests
{
    private readonly PromptSchemaValidator _validator;

    public PromptSchemaValidatorTests()
    {
        _validator = new PromptSchemaValidator(NullLogger<PromptSchemaValidator>.Instance);
    }

    [Fact]
    public void Validate_ValidPrompt_ReturnsNoErrors()
    {
        // Arrange
        var prompt = new PromptDefinition
        {
            Version = "v1.0.0",
            Name = "test",
            Model = "gpt-4o",
            Schema = "intent-classifier",
            SystemMessage = "You are a test system",
            EnumIntents = new List<string> { "Intent1", "Intent2" }
        };

        // Act
        var errors = _validator.Validate(prompt, "test.json");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingVersion_ReturnsError()
    {
        // Arrange
        var prompt = new PromptDefinition
        {
            Version = "",
            Name = "test",
            Model = "gpt-4o",
            Schema = "test",
            SystemMessage = "Test"
        };

        // Act
        var errors = _validator.Validate(prompt, "test.json");

        // Assert
        Assert.Contains(errors, e => e.Contains("Version is required"));
    }

    [Fact]
    public void Validate_InvalidSemanticVersion_ReturnsError()
    {
        // Arrange
        var prompt = new PromptDefinition
        {
            Version = "1.0",  // Invalid - should be X.Y.Z
            Name = "test",
            Model = "gpt-4o",
            Schema = "test",
            SystemMessage = "Test"
        };

        // Act
        var errors = _validator.Validate(prompt, "test.json");

        // Assert
        Assert.Contains(errors, e => e.Contains("not a valid semantic version"));
    }

    [Fact]
    public void Validate_IntentClassifierWithoutEnumIntents_ReturnsError()
    {
        // Arrange
        var prompt = new PromptDefinition
        {
            Version = "v1.0.0",
            Name = "classify",
            Model = "gpt-4o",
            Schema = "intent-classifier",
            SystemMessage = "Test",
            EnumIntents = null
        };

        // Act
        var errors = _validator.Validate(prompt, "test.json");

        // Assert
        Assert.Contains(errors, e => e.Contains("EnumIntents"));
    }

    [Fact]
    public void Validate_InvalidTemperature_ReturnsError()
    {
        // Arrange
        var prompt = new PromptDefinition
        {
            Version = "v1.0.0",
            Name = "test",
            Model = "gpt-4o",
            Schema = "test",
            SystemMessage = "Test",
            Temperature = 3.0f  // Invalid - should be 0-2
        };

        // Act
        var errors = _validator.Validate(prompt, "test.json");

        // Assert
        Assert.Contains(errors, e => e.Contains("Temperature must be between 0 and 2"));
    }

    [Fact]
    public void Validate_InvalidMaxTokens_ReturnsError()
    {
        // Arrange
        var prompt = new PromptDefinition
        {
            Version = "v1.0.0",
            Name = "test",
            Model = "gpt-4o",
            Schema = "test",
            SystemMessage = "Test",
            MaxTokens = -10
        };

        // Act
        var errors = _validator.Validate(prompt, "test.json");

        // Assert
        Assert.Contains(errors, e => e.Contains("MaxTokens must be positive"));
    }

    [Fact]
    public void IsValidSemanticVersion_ValidVersions_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(_validator.IsValidSemanticVersion("1.0.0"));
        Assert.True(_validator.IsValidSemanticVersion("v1.0.0"));
        Assert.True(_validator.IsValidSemanticVersion("2.3.5"));
        Assert.True(_validator.IsValidSemanticVersion("v10.20.30"));
    }

    [Fact]
    public void IsValidSemanticVersion_InvalidVersions_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(_validator.IsValidSemanticVersion("1.0"));
        Assert.False(_validator.IsValidSemanticVersion("1"));
        Assert.False(_validator.IsValidSemanticVersion("1.0.0.0"));
        Assert.False(_validator.IsValidSemanticVersion("abc"));
        Assert.False(_validator.IsValidSemanticVersion(""));
    }

    [Fact]
    public void NormalizeVersion_ValidVersions_ReturnsNormalized()
    {
        // Arrange & Act & Assert
        Assert.Equal("v1.0.0", _validator.NormalizeVersion("1.0.0"));
        Assert.Equal("v1.0.0", _validator.NormalizeVersion("v1.0.0"));
        Assert.Equal("v2.3.5", _validator.NormalizeVersion("V2.3.5"));
    }

    [Fact]
    public void NormalizeVersion_InvalidVersion_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidPromptVersionException>(() => _validator.NormalizeVersion("1.0"));
        Assert.Throws<InvalidPromptVersionException>(() => _validator.NormalizeVersion("invalid"));
    }

    [Fact]
    public void ValidateAndThrow_InvalidPrompt_ThrowsPromptValidationException()
    {
        // Arrange
        var prompt = new PromptDefinition
        {
            Version = "",  // Invalid
            Name = "test",
            Model = "gpt-4o",
            Schema = "test",
            SystemMessage = "Test"
        };

        // Act & Assert
        var exception = Assert.Throws<PromptValidationException>(() => 
            _validator.ValidateAndThrow(prompt, "test.json"));
        
        Assert.NotEmpty(exception.ValidationErrors);
    }

    [Fact]
    public void Validate_ExamplesWithMissingInput_ReturnsError()
    {
        // Arrange
        var prompt = new PromptDefinition
        {
            Version = "v1.0.0",
            Name = "test",
            Model = "gpt-4o",
            Schema = "test",
            SystemMessage = "Test",
            Examples = new List<PromptExample>
            {
                new PromptExample { Input = "", Output = "Test" }
            }
        };

        // Act
        var errors = _validator.Validate(prompt, "test.json");

        // Assert
        Assert.Contains(errors, e => e.Contains("Input is required"));
    }
}
