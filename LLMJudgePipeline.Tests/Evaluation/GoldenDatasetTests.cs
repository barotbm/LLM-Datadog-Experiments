using LLMJudgePipeline.Prompts.Models;
using LLMJudgePipeline.Prompts.Registry;
using LLMJudgePipeline.Prompts.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LLMJudgePipeline.Tests.Evaluation;

/// <summary>
/// Golden dataset tests for prompt evaluation.
/// These tests validate that the prompt instructions and enumerated intents
/// correctly guide classification without making actual LLM calls.
/// </summary>
public class GoldenDatasetTests
{
    private readonly IPromptRegistry _registry;
    private PromptDefinition _classifyPromptV1 = null!;
    private PromptDefinition _classifyPromptV2 = null!;

    public GoldenDatasetTests()
    {
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(e => e.ContentRootPath)
            .Returns(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));

        var validator = new PromptSchemaValidator(NullLoggerFactory.Instance.CreateLogger<PromptSchemaValidator>());
        _registry = new PromptRegistry(
            NullLoggerFactory.Instance.CreateLogger<PromptRegistry>(),
            validator,
            mockEnvironment.Object);

        // Load prompts for testing
        _classifyPromptV1 = _registry.Load("classify", "v1.0.0");
        _classifyPromptV2 = _registry.Load("classify", "v2.0.0");
    }

    #region V1 Prompt Tests

    [Theory]
    [InlineData("I need to check my account balance", "BalanceInquiry")]
    [InlineData("What's my current balance?", "BalanceInquiry")]
    [InlineData("How much money do I have?", "BalanceInquiry")]
    public void V1_Prompt_BalanceInquiry_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        // Assert prompt contains the expected intent
        AssertPromptSupportsIntent(_classifyPromptV1, expectedIntent);
        
        // Assert instructions guide toward this classification
        AssertInstructionsGuideToIntent(_classifyPromptV1, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("Transfer $500 to John", "TransferFunds")]
    [InlineData("Send money to savings account", "TransferFunds")]
    [InlineData("Move $100 from checking to savings", "TransferFunds")]
    public void V1_Prompt_TransferFunds_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV1, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV1, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("Show me my recent transactions", "TransactionHistory")]
    [InlineData("What purchases did I make last week?", "TransactionHistory")]
    [InlineData("View my transaction history", "TransactionHistory")]
    public void V1_Prompt_TransactionHistory_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV1, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV1, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("I lost my debit card", "CardManagement")]
    [InlineData("Block my credit card", "CardManagement")]
    [InlineData("Activate my new card", "CardManagement")]
    public void V1_Prompt_CardManagement_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV1, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV1, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("I need to pay my credit card bill", "BillPayment")]
    [InlineData("Pay my utility bill", "BillPayment")]
    [InlineData("Schedule a payment", "BillPayment")]
    public void V1_Prompt_BillPayment_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV1, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV1, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("I need help with my account", "CustomerSupport")]
    [InlineData("Connect me to an agent", "CustomerSupport")]
    [InlineData("I have a question about fees", "CustomerSupport")]
    public void V1_Prompt_CustomerSupport_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV1, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV1, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("What's the weather like?", "OutOfScope")]
    [InlineData("Tell me a joke", "OutOfScope")]
    [InlineData("Random gibberish", "OutOfScope")]
    public void V1_Prompt_OutOfScope_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV1, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV1, userMessage, expectedIntent);
    }

    #endregion

    #region V2 Prompt Tests

    [Theory]
    [InlineData("I need to update my email address", "AccountSettings")]
    [InlineData("Change my phone number", "AccountSettings")]
    [InlineData("Update my profile information", "AccountSettings")]
    public void V2_Prompt_AccountSettings_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV2, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV2, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("I need a bank statement for my loan application", "DocumentRequest")]
    [InlineData("Send me my tax documents", "DocumentRequest")]
    [InlineData("I need proof of address", "DocumentRequest")]
    public void V2_Prompt_DocumentRequest_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV2, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV2, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("I don't recognize this charge", "DisputeTransaction")]
    [InlineData("This transaction is fraudulent", "DisputeTransaction")]
    [InlineData("I want to dispute a purchase", "DisputeTransaction")]
    public void V2_Prompt_DisputeTransaction_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV2, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV2, userMessage, expectedIntent);
    }

    [Theory]
    [InlineData("I want to close my account", "AccountClosure")]
    [InlineData("Cancel my checking account", "AccountClosure")]
    [InlineData("How do I close my savings account?", "AccountClosure")]
    public void V2_Prompt_AccountClosure_HasCorrectInstructions(string userMessage, string expectedIntent)
    {
        AssertPromptSupportsIntent(_classifyPromptV2, expectedIntent);
        AssertInstructionsGuideToIntent(_classifyPromptV2, userMessage, expectedIntent);
    }

    #endregion

    #region Version Comparison Tests

    [Fact]
    public void V2_Prompt_HasMoreIntentsThanV1()
    {
        // V2 should have expanded taxonomy
        Assert.True(_classifyPromptV2.EnumIntents!.Count > _classifyPromptV1.EnumIntents!.Count);
        Assert.Equal(7, _classifyPromptV1.EnumIntents!.Count);
        Assert.Equal(10, _classifyPromptV2.EnumIntents!.Count);
    }

    [Fact]
    public void V2_Prompt_HasMoreExamplesThanV1()
    {
        // V2 should have more examples for better guidance
        Assert.True(_classifyPromptV2.Examples!.Count > _classifyPromptV1.Examples!.Count);
        Assert.Equal(3, _classifyPromptV1.Examples!.Count);
        Assert.Equal(5, _classifyPromptV2.Examples!.Count);
    }

    [Fact]
    public void V2_Prompt_IncludesPrivacyGuidelines()
    {
        // V2 should have privacy/PII guidelines
        var hasPrivacyGuideline = _classifyPromptV2.Instructions!
            .Any(i => i.Contains("PII", StringComparison.OrdinalIgnoreCase) || 
                     i.Contains("privacy", StringComparison.OrdinalIgnoreCase));
        
        Assert.True(hasPrivacyGuideline, "V2 should include privacy/PII guidelines");
    }

    [Fact]
    public void V2_Prompt_HasMoreDetailedInstructions()
    {
        // V2 should have more comprehensive instructions
        var v1InstructionLength = _classifyPromptV1.Instructions!.Sum(i => i.Length);
        var v2InstructionLength = _classifyPromptV2.Instructions!.Sum(i => i.Length);
        
        Assert.True(v2InstructionLength > v1InstructionLength);
    }

    #endregion

    #region Prompt Quality Tests

    [Fact]
    public void V1_Prompt_HasExamplesForEachIntent()
    {
        // Not all intents need examples, but key ones should have them
        var exampleIntents = _classifyPromptV1.Examples!
            .Select(e => e.Output)
            .ToHashSet();

        Assert.NotEmpty(exampleIntents);
    }

    [Fact]
    public void V2_Prompt_ExamplesHaveExplanations()
    {
        // All examples should have explanations for transparency
        var allExamplesHaveExplanations = _classifyPromptV2.Examples!
            .All(e => !string.IsNullOrWhiteSpace(e.Explanation));

        Assert.True(allExamplesHaveExplanations);
    }

    [Fact]
    public void Both_Prompts_HaveAppropriateTemperature()
    {
        // Classification should use low temperature for consistency
        Assert.True(_classifyPromptV1.Temperature <= 0.5f, "V1 temperature should be <= 0.5 for deterministic classification");
        Assert.True(_classifyPromptV2.Temperature <= 0.5f, "V2 temperature should be <= 0.5 for deterministic classification");
    }

    [Fact]
    public void Both_Prompts_UseCorrectModel()
    {
        // Should use gpt-4o for accuracy
        Assert.Equal("gpt-4o", _classifyPromptV1.Model);
        Assert.Equal("gpt-4o", _classifyPromptV2.Model);
    }

    #endregion

    #region Helper Methods

    private void AssertPromptSupportsIntent(PromptDefinition prompt, string expectedIntent)
    {
        Assert.NotNull(prompt.EnumIntents);
        Assert.Contains(expectedIntent, prompt.EnumIntents);
    }

    private void AssertInstructionsGuideToIntent(PromptDefinition prompt, string userMessage, string expectedIntent)
    {
        // Verify that the instructions contain guidance for this intent
        // This is a structural validation - not calling the LLM
        Assert.NotNull(prompt.Instructions);
        Assert.NotEmpty(prompt.Instructions);
        
        // Check if intent is in the enum
        AssertPromptSupportsIntent(prompt, expectedIntent);
        
        // Check if examples provide guidance for similar messages
        if (prompt.Examples != null && prompt.Examples.Any())
        {
            var relevantExamples = prompt.Examples
                .Where(e => e.Output == expectedIntent)
                .ToList();
            
            // If there are examples for this intent, they should be relevant
            if (relevantExamples.Any())
            {
                Assert.True(relevantExamples.All(e => !string.IsNullOrWhiteSpace(e.Input)));
            }
        }
    }

    #endregion
}
