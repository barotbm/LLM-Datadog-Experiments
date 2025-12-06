# Quick Reference Guide

## Project Files Overview

```
LLM-Datadog-Experiments/
│
├── LLMJudgePipeline.sln                    # Solution file
├── README.md                                # Project overview
├── ARCHITECTURE.md                          # Architecture documentation
├── SETUP.md                                 # Setup and deployment guide
├── PROMPTS.md                               # LLM prompts and examples
├── TEST_REQUESTS.md                         # API test examples
├── .gitignore                               # Git ignore rules
│
└── LLMJudgePipeline/                        # Main project
    ├── LLMJudgePipeline.csproj              # Project file
    ├── Program.cs                           # Application entry point & DI setup
    ├── appsettings.json                     # Configuration
    ├── appsettings.Development.json         # Dev configuration
    │
    ├── Controllers/
    │   └── InferenceController.cs           # API endpoint: POST /api/infer
    │
    ├── Services/
    │   ├── InferenceService.cs              # Main pipeline orchestrator
    │   │
    │   ├── Generators/
    │   │   └── Gpt4oGenerator.cs            # GPT-4o intent classifier
    │   │
    │   ├── Judges/
    │   │   ├── Gpt4oMiniJudge.cs            # Fast judge (gpt-4o-mini)
    │   │   ├── Gpt41MiniJudge.cs            # Balanced judge (gpt-4.1-mini)
    │   │   ├── Gpt4oStrongJudge.cs          # Accurate judge (gpt-4o)
    │   │   └── ExperimentAwareJudge.cs      # Experiment-driven judge selector
    │   │
    │   ├── Routing/
    │   │   └── RiskBasedJudgeRouter.cs      # When to invoke judge
    │   │
    │   ├── Drift/
    │   │   └── DriftEvaluator.cs            # Drift detection logic
    │   │
    │   └── Embedding/
    │       └── EmbeddingService.cs          # Cosine similarity calculations
    │
    ├── Infrastructure/
    │   ├── DatadogMetricsService.cs         # Datadog metrics integration
    │   └── DatadogExperimentsService.cs     # Datadog experiments integration
    │
    ├── Interfaces/
    │   ├── ILlmGenerator.cs                 # Generator abstraction
    │   ├── ILlmJudge.cs                     # Judge abstraction
    │   ├── IJudgeRouter.cs                  # Router abstraction
    │   ├── IDriftEvaluator.cs               # Drift evaluator abstraction
    │   ├── IEmbeddingService.cs             # Embedding service abstraction
    │   ├── IInferenceService.cs             # Inference service abstraction
    │   ├── IDatadogMetrics.cs               # Metrics abstraction
    │   └── IDatadogExperiments.cs           # Experiments abstraction
    │
    ├── Models/
    │   ├── GeneratorResult.cs               # Generator output model
    │   ├── JudgeResult.cs                   # Judge output model
    │   ├── DriftEvalResult.cs               # Drift evaluation model
    │   ├── InferenceRequest.cs              # API request model
    │   └── InferenceResponse.cs             # API response model
    │
    └── Properties/
        └── launchSettings.json              # Launch configuration
```

## Quick Commands

### Build & Run
```bash
# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run application
dotnet run

# Run with auto-reload
dotnet watch run

# Run tests (when added)
dotnet test
```

### Configuration
```bash
# Set OpenAI API key (user secrets)
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key"

# Set OpenAI API key (environment variable)
$env:OpenAI__ApiKey = "sk-your-key"
```

## API Quick Reference

### POST /api/infer
**Purpose:** Process user message and return intent with validation

**Request:**
```json
{
  "message": "string (required)",
  "sessionId": "string (optional)"
}
```

**Response:**
```json
{
  "intent": "string",
  "confidence": "number (0-1)",
  "judgeValid": "boolean",
  "driftDetected": "boolean",
  "variantUsed": "string",
  "judgeSkipped": "boolean",
  "routingStrategy": "string"
}
```

### GET /api/health
**Purpose:** Health check endpoint

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "ISO 8601 datetime"
}
```

## Intent Types

| Intent | Description | Example Message |
|--------|-------------|----------------|
| PayoffQuote | User wants payoff amount | "What's my payoff amount?" |
| Payment | User wants to make payment | "I need to make a payment" |
| AddressChange | User wants to update address | "I moved to a new address" |
| BalanceInquiry | User asks about balance | "What's my balance?" |
| RateInformation | User asks about rates | "What's my interest rate?" |
| GeneralInquiry | General questions | "Can you help me?" |
| Other | Anything else | "Random question" |

## Judge Variants

| Variant | Model | Speed | Cost | Accuracy |
|---------|-------|-------|------|----------|
| gpt4o-mini | GPT-4o-mini | Fast | Low | Good |
| gpt41-mini | GPT-4.1-mini | Medium | Medium | Better |
| gpt4o-strong | GPT-4o | Slow | High | Best |

## Routing Strategies

| Strategy | When Judge is Invoked |
|----------|---------------------|
| always | Every request |
| sensitive-only | PayoffQuote, Payment, AddressChange |
| low-confidence | Generator confidence < 0.75 |

## Datadog Metrics Reference

### Generator Metrics
- `llm.generator.invoked` - Count of calls
- `llm.generator.latency_ms` - Response time

### Judge Metrics
- `llm.judge.accept` - Accepted count
- `llm.judge.reject` - Rejected count
- `llm.judge.violation` - Violations by type
- `llm.judge.latency_ms` - Evaluation time
- `llm.judge.invoked` - Invocation count
- `llm.judge.skipped` - Skipped count

### Drift Metrics
- `llm.drift.evaluated` - Drift checks

### Pipeline Metrics
- `llm.inference.completed` - Successful pipelines
- `llm.inference.error` - Errors
- `llm.fallback.triggered` - Fallback activations

## Common Tags

- `experiment:llm-judge-model` - Experiment name
- `variant:{name}` - Selected variant
- `intent:{intent}` - Predicted intent
- `judge_model:{model}` - Judge model used
- `violation_type:{type}` - Violation type
- `drift_detected:{true|false}` - Drift status
- `routing_strategy:{strategy}` - Routing strategy
- `confidence_bucket:{high|medium|low|very_low}` - Confidence level

## Configuration Reference

### appsettings.json
```json
{
  "OpenAI": {
    "ApiKey": "sk-..."  // Your OpenAI API key
  },
  "Datadog": {
    "Host": "localhost",  // Datadog agent host
    "Port": "8125"        // DogStatsD port
  },
  "Experiments": {
    "JudgeModel": "gpt4o-mini",       // Default judge variant
    "RoutingStrategy": "sensitive-only" // Default routing strategy
  }
}
```

## Dependency Injection Map

```csharp
// Singleton (app lifetime)
IDatadogMetrics → DatadogMetricsService
IDatadogExperiments → DatadogExperimentsService
OpenAIClient → new OpenAIClient(apiKey)

// Scoped (per request)
ILlmGenerator → Gpt4oGenerator
IEmbeddingService → EmbeddingService
IDriftEvaluator → DriftEvaluator
IJudgeRouter → RiskBasedJudgeRouter
IInferenceService → InferenceService
Gpt4oMiniJudge
Gpt41MiniJudge
Gpt4oStrongJudge
ExperimentAwareJudge
```

## Typical Pipeline Execution

```
1. Receive POST /api/infer request
2. InferenceService.ProcessAsync()
3. Call Gpt4oGenerator → OpenAI (gpt-4o)
4. Call DriftEvaluator
   └─ EmbeddingService → OpenAI (ada-002)
5. Call RiskBasedJudgeRouter
   ├─ Check experiment: judge-routing-strategy
   └─ If should evaluate:
      └─ ExperimentAwareJudge
         ├─ Check experiment: llm-judge-model
         └─ Selected judge → OpenAI
6. Log metrics to Datadog
7. Return InferenceResponse
```

## Error Handling

### Generator Errors
- Returns intent="Error", confidence=0.0
- Pipeline continues with error result

### Judge Errors
- Returns isValid=false, violation="error"
- Triggers fallback to "RequiresReview"

### Drift Evaluator Errors
- Returns driftDetected=false (fail-open)
- Pipeline continues

### API Errors
- 400 Bad Request - Invalid input
- 500 Internal Server Error - System error

## Performance Tips

### Reduce Latency
1. Use `gpt4o-mini` judge variant
2. Set routing to `sensitive-only`
3. Cache embeddings for frequent messages
4. Use connection pooling

### Reduce Cost
1. Use `low-confidence` routing strategy
2. Implement request deduplication
3. Monitor token usage metrics
4. Set appropriate confidence thresholds

### Increase Accuracy
1. Use `gpt4o-strong` judge variant
2. Set routing to `always`
3. Lower confidence threshold to 0.85
4. Tune drift detection parameters

## Troubleshooting Checklist

### Application Won't Start
- [ ] .NET 8.0 SDK installed?
- [ ] All packages restored? (`dotnet restore`)
- [ ] OpenAI API key configured?
- [ ] Port 5000/5001 available?

### No Metrics in Datadog
- [ ] Datadog agent running?
- [ ] Correct host/port in config?
- [ ] DogStatsD enabled?
- [ ] Check agent logs

### High Latency
- [ ] Check OpenAI API status
- [ ] Review routing strategy (too many judge calls?)
- [ ] Monitor network latency
- [ ] Check Datadog metrics for bottlenecks

### Judge Always Skipped
- [ ] Verify routing strategy variant
- [ ] Check intent classification
- [ ] Review confidence scores
- [ ] Examine Datadog experiment config

## Testing Checklist

### Manual Testing
- [ ] Generator returns valid intent
- [ ] Judge evaluates correctly
- [ ] Drift detection works
- [ ] Metrics appear in Datadog
- [ ] All routing strategies work
- [ ] All judge variants work

### Integration Testing
- [ ] Sequential requests maintain conversation history
- [ ] State transitions validated correctly
- [ ] Experiment variants distributed properly
- [ ] Fallback triggered on rejection

## Next Steps After Setup

1. **Configure OpenAI API key** in appsettings.json or user secrets
2. **Run the application** with `dotnet run`
3. **Test the API** using Swagger UI at https://localhost:5001/swagger
4. **Set up Datadog agent** (optional) for metrics
5. **Configure experiments** in Datadog UI or use mock implementation
6. **Monitor metrics** in Datadog dashboards
7. **Tune parameters** based on performance and accuracy requirements

## Support & Resources

- **OpenAI Documentation:** https://platform.openai.com/docs
- **Datadog Documentation:** https://docs.datadoghq.com
- **ASP.NET Core Documentation:** https://docs.microsoft.com/aspnet/core
- **Project README:** See README.md for detailed overview
- **Architecture Guide:** See ARCHITECTURE.md for design details
- **Setup Guide:** See SETUP.md for deployment instructions
