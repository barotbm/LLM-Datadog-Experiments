using LLMJudgePipeline.Prompts.Exceptions;
using LLMJudgePipeline.Prompts.Registry;
using LLMJudgePipeline.Prompts.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LLMJudgePipeline.Tests.Prompts;

public class PromptRegistryTests : IDisposable
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly PromptSchemaValidator _validator;
    private readonly PromptRegistry _registry;
    private readonly string _testPromptsPath;

    public PromptRegistryTests()
    {
        _testPromptsPath = Path.Combine(Directory.GetCurrentDirectory(), "TestPrompts");
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        _validator = new PromptSchemaValidator(NullLoggerFactory.Instance.CreateLogger<PromptSchemaValidator>());
        _registry = new PromptRegistry(
            NullLoggerFactory.Instance.CreateLogger<PromptRegistry>(),
            _validator,
            _mockEnvironment.Object);
    }

    [Fact]
    public void Load_ValidPrompt_ReturnsPromptDefinition()
    {
        // Act
        var prompt = _registry.Load("test-prompt", "v1.0.0");

        // Assert
        Assert.NotNull(prompt);
        Assert.Equal("1.0.0", prompt.Version);
        Assert.Equal("test-prompt", prompt.Name);
        Assert.Equal("gpt-4o", prompt.Model);
        Assert.Equal(0.5f, prompt.Temperature);
        Assert.Equal(100, prompt.MaxTokens);
    }

    [Fact]
    public void Load_NonExistentPrompt_ThrowsPromptNotFoundException()
    {
        // Act & Assert
        Assert.Throws<PromptNotFoundException>(() => 
            _registry.Load("nonexistent", "v1.0.0"));
    }

    [Fact]
    public void Load_InvalidVersion_ThrowsInvalidPromptVersionException()
    {
        // Act & Assert
        Assert.Throws<InvalidPromptVersionException>(() => 
            _registry.Load("test-prompt", "1.0"));
    }

    [Fact]
    public void Load_PromptWithInvalidSchema_ThrowsPromptValidationException()
    {
        // Act & Assert
        Assert.Throws<PromptValidationException>(() => 
            _registry.Load("invalid-prompt", "v1.0.0"));
    }

    [Fact]
    public void Load_SamePromptTwice_ReturnsCachedVersion()
    {
        // Act
        var prompt1 = _registry.Load("test-prompt", "v1.0.0");
        var prompt2 = _registry.Load("test-prompt", "v1.0.0");

        // Assert
        Assert.Same(prompt1, prompt2);
    }

    [Fact]
    public void TryLoad_NonExistentPrompt_ReturnsNull()
    {
        // Act
        var prompt = _registry.TryLoad("nonexistent", "v1.0.0");

        // Assert
        Assert.Null(prompt);
    }

    [Fact]
    public void TryLoad_ValidPrompt_ReturnsPrompt()
    {
        // Act
        var prompt = _registry.TryLoad("test-prompt", "v1.0.0");

        // Assert
        Assert.NotNull(prompt);
        Assert.Equal("test-prompt", prompt.Name);
    }

    [Fact]
    public void ListVersions_ExistingPrompt_ReturnsVersions()
    {
        // Act
        var versions = _registry.ListVersions("test-prompt");

        // Assert
        Assert.Contains("v1.5.0", versions);
        Assert.Contains("v1.0.0", versions);
        Assert.Equal(2, versions.Count);
    }

    [Fact]
    public void GetLatestVersion_ExistingPrompt_ReturnsLatest()
    {
        // Act
        var latestVersion = _registry.GetLatestVersion("test-prompt");

        // Assert
        Assert.Equal("v1.5.0", latestVersion);
    }

    [Fact]
    public void LoadLatest_ExistingPrompt_ReturnsLatestVersion()
    {
        // Act
        var prompt = _registry.LoadLatest("test-prompt");

        // Assert
        Assert.NotNull(prompt);
        Assert.Equal("1.5.0", prompt.Version);
        Assert.Equal("gpt-4o-mini", prompt.Model);
    }

    [Fact]
    public void ClearCache_SpecificPrompt_RemovesFromCache()
    {
        // Arrange
        var prompt1 = _registry.Load("test-prompt", "v1.0.0");

        // Act
        _registry.ClearCache("test-prompt", "v1.0.0");
        var prompt2 = _registry.Load("test-prompt", "v1.0.0");

        // Assert
        Assert.NotSame(prompt1, prompt2);
    }

    [Fact]
    public void ClearCache_All_RemovesAllFromCache()
    {
        // Arrange
        var prompt1 = _registry.Load("test-prompt", "v1.0.0");

        // Act
        _registry.ClearCache();
        var prompt2 = _registry.Load("test-prompt", "v1.0.0");

        // Assert
        Assert.NotSame(prompt1, prompt2);
    }

    [Fact]
    public void Warmup_MultiplePrompts_PreloadsCache()
    {
        // Arrange
        var promptVersions = new Dictionary<string, string>
        {
            { "test-prompt", "v1.0.0" },
            { "test-prompt", "v1.5.0" }
        };

        // Act
        _registry.Warmup(promptVersions);

        // Assert - Prompts should be cached (verified by successful load)
        var prompt1 = _registry.Load("test-prompt", "v1.0.0");
        var prompt2 = _registry.Load("test-prompt", "v1.5.0");
        
        Assert.NotNull(prompt1);
        Assert.NotNull(prompt2);
    }

    [Fact]
    public async Task LoadAsync_ValidPrompt_ReturnsPromptDefinition()
    {
        // Act
        var prompt = await _registry.LoadAsync("test-prompt", "v1.0.0");

        // Assert
        Assert.NotNull(prompt);
        Assert.Equal("test-prompt", prompt.Name);
    }

    public void Dispose()
    {
        _registry.ClearCache();
    }
}
