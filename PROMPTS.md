# Prompt Versioning System

This document describes the production-grade prompt versioning system implemented in the LLM Judge Pipeline.

## Overview

The prompt versioning system provides:
- **Semantic Versioning**: All prompts use vX.Y.Z versioning
- **JSON Storage**: Structured prompt definitions with metadata
- **Schema Validation**: Automatic validation of prompt structure and parameters
- **Registry Pattern**: Centralized prompt loading with caching
- **Configuration-Driven**: Version selection via appsettings.json
- **Testing Infrastructure**: Comprehensive unit and evaluation tests

## Directory Structure

```
LLMJudgePipeline/Prompts/
├── Intents/
│   ├── classify-v1.0.0.prompt.json      # Initial version
│   └── classify-v2.0.0.prompt.json      # Enhanced taxonomy
├── Guardrails/
│   └── judge-evaluation-v1.0.0.prompt.json
├── Summaries/
│   └── summary-v1.0.0.prompt.json
├── Models/
│   ├── PromptDefinition.cs              # Core prompt model
│   └── PromptVersionConfiguration.cs     # Configuration binding
├── Validation/
│   └── PromptSchemaValidator.cs         # Schema validation logic
├── Registry/
│   ├── IPromptRegistry.cs               # Registry interface
│   └── PromptRegistry.cs                # Implementation with caching
├── Extensions/
│   └── PromptServiceExtensions.cs       # DI registration
└── Exceptions/
    └── PromptExceptions.cs              # Custom exceptions
```

## Prompt Schema

Each prompt JSON file follows this structure:

```json
{
  "version": "1.0.0",
  "name": "classify",
  "model": "gpt-4o",
  "schema": "intent-classifier",
  "systemMessage": "You are an intent classification system...",
  "instructions": [
    "Analyze the user's message",
    "Extract key entities",
    "Classify into one of the predefined intents"
  ],
  "metadata": {
    "author": "LLM Team",
    "purpose": "Customer service intent routing"
  },
  "enumIntents": [
    "BalanceInquiry",
    "TransferFunds",
    "TransactionHistory"
  ],
  "temperature": 0.3,
  "maxTokens": 150,
  "examples": [
    {
      "input": "What's my balance?",
      "output": "BalanceInquiry",
      "explanation": "Direct request for account balance information"
    }
  ],
  "createdDate": "2024-01-15T10:00:00Z",
  "changelog": [
    {
      "version": "1.0.0",
      "date": "2024-01-15",
      "changes": "Initial release with 7 intents"
    }
  ]
}
```

## Schema Types

### intent-classifier
Required fields:
- `enumIntents`: Array of valid intent strings
- `temperature`: 0.0 - 2.0 (recommend ≤ 0.5 for deterministic classification)
- `maxTokens`: Positive integer
- `examples`: Array of input/output/explanation examples

### guardrail
Required fields:
- `temperature`: Low value (0.0 - 0.2) for consistency
- `systemMessage`: Evaluation criteria
- `instructions`: Specific checks to perform

### summary
Required fields:
- `temperature`: 0.3 - 0.7 for balanced creativity
- `maxTokens`: Adequate for summary length
- `instructions`: Summarization guidelines

## Example Prompts

### Generator System Prompt (classify-v2.0.0.prompt.json)

```
You are an intent classification system for a banking customer service platform.
Analyze the user's message and return a JSON response with the following structure:
{
  "intent": "<intent_name>",
  "confidence": <0.0-1.0>,
  "entities": {
    "key": "value"
  }
}

Valid intents:
- BalanceInquiry: User wants to check account balance
- TransferFunds: User wants to transfer money
- TransactionHistory: User wants to view past transactions
- CardManagement: User needs help with debit/credit cards
- BillPayment: User wants to pay bills
- AccountSettings: User wants to update account info
- CustomerSupport: User needs general help
- DocumentRequest: User needs bank statements or documents
- DisputeTransaction: User disputes a charge
- AccountClosure: User wants to close account
- OutOfScope: Non-banking queries

Extract relevant entities like amounts, dates, account numbers, etc.
Remember to redact PII like account numbers, SSN, etc.
```

### Judge System Prompt (judge-evaluation-v1.0.0.prompt.json)

All judges use this system prompt:

```
You are a quality evaluation system for LLM-generated intent classifications.
Evaluate whether the predicted intent is valid, safe, and correct for the given user message.

Check for:
1. Safety violations (harmful, inappropriate content)
2. Compliance issues (privacy, regulations)
3. Correctness (does the intent match the user's request?)

Return JSON with this exact structure:
{
  "is_valid": true/false,
  "violation": "none" or "safety" or "compliance" or "correctness",
  "confidence": <0.0-1.0>
}
```

## Example User Messages and Expected Outputs

### 1. Payoff Request
**User:** "I want to know my payoff amount"
**Expected Output:**
```json
{
  "intent": "PayoffQuote",
  "confidence": 0.95,
  "entities": {}
}
```

### 2. Payment Request
**User:** "I'd like to make a payment of $500"
**Expected Output:**
```json
{
  "intent": "Payment",
  "confidence": 0.92,
  "entities": {
    "amount": "500"
  }
}
```

### 3. Address Change
**User:** "I need to update my mailing address"
**Expected Output:**
```json
{
  "intent": "AddressChange",
  "confidence": 0.90,
  "entities": {}
}
```

### 4. Balance Inquiry
**User:** "What's my current balance?"
**Expected Output:**
```json
{
  "intent": "BalanceInquiry",
  "confidence": 0.93,
  "entities": {}
}
```

### 5. Ambiguous Request
**User:** "Help me with something"
**Expected Output:**
```json
{
  "intent": "GeneralInquiry",
  "confidence": 0.60,
  "entities": {}
}
```

## Judge Evaluation Examples

### Valid Request
**User Message:** "I want to know my payoff amount"
**Predicted Intent:** "PayoffQuote"
**Judge Output:**
```json
{
  "is_valid": true,
  "violation": "none",
  "confidence": 0.95
}
```

### Invalid Request - Correctness
**User Message:** "What's the weather today?"
**Predicted Intent:** "PayoffQuote"
**Judge Output:**
```json
{
  "is_valid": false,
  "violation": "correctness",
  "confidence": 0.88
}
```

### Invalid Request - Safety
**User Message:** "How do I hack into the system?"
**Predicted Intent:** "GeneralInquiry"
**Judge Output:**
```json
{
  "is_valid": false,
  "violation": "safety",
  "confidence": 0.92
}
```

## Configuration

Set prompt versions in `appsettings.json`:

```json
{
  "PromptVersions": {
    "IntentClassifier": "v2.0.0",
    "JudgeEvaluation": "v1.0.0",
    "SummaryGenerator": "v1.0.0",
    "Guardrail": "v1.0.0"
  }
}
```

## Testing

### Schema Validation Tests
```bash
dotnet test --filter "FullyQualifiedName~PromptSchemaValidatorTests"
```

### Registry Tests
```bash
dotnet test --filter "FullyQualifiedName~PromptRegistryTests"
```

### Golden Dataset Tests
```bash
dotnet test --filter "FullyQualifiedName~GoldenDatasetTests"
```

### All Prompt Tests
```bash
dotnet test --filter "FullyQualifiedName~Prompts|Evaluation"
```

## CI/CD Integration

GitHub Actions workflow (`.github/workflows/prompt-tests.yml`) runs on PR:

1. **Schema Validation**: Validates JSON format and semantic versioning
2. **Unit Tests**: Tests validator and registry functionality
3. **Golden Dataset Evaluation**: Verifies prompt instructions guide correct classification
4. **JSON Linting**: Ensures valid JSON and filename conventions

## Best Practices

1. **Version Control**: Always increment version for any change
2. **Examples**: Include 3-5 diverse examples per intent
3. **Temperature**: 
   - Classification: ≤ 0.5
   - Evaluation: ≤ 0.2
   - Generation: 0.3 - 0.7
4. **Token Limits**: Set appropriate maxTokens for task
5. **Metadata**: Document author, purpose, and changes
6. **Testing**: Add golden dataset tests for new intents
7. **Changelog**: Maintain version history in prompt file
