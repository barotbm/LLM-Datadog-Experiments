# LLM Judge Pipeline

A complete C# backend implementing an LLM-as-a-Judge pipeline with Datadog Experiments integration.

## Architecture

This project implements a clean architecture pattern with the following components:

### Core Components

1. **Generator** (`Services/Generators/`)
   - `Gpt4oGenerator`: Intent classification using GPT-4o
   - Returns structured JSON with intent, confidence, and entities

2. **Judges** (`Services/Judges/`)
   - `Gpt4oMiniJudge`: Fast, efficient judge using GPT-4o-mini
   - `Gpt41MiniJudge`: Balanced judge using GPT-4.1-mini
   - `Gpt4oStrongJudge`: High-quality judge using GPT-4o
   - `ExperimentAwareJudge`: Dynamically selects judge based on Datadog experiments

3. **Routing** (`Services/Routing/`)
   - `RiskBasedJudgeRouter`: Determines when to invoke the judge based on:
     - Always: Every request
     - Sensitive-only: Only for PayoffQuote, Payment, AddressChange
     - Low-confidence: When generator confidence < 0.75

4. **Drift Detection** (`Services/Drift/`)
   - `DriftEvaluator`: Detects anomalies using:
     - Embedding similarity between message and intent
     - Domain consistency checks
     - State machine validation

5. **Embedding Service** (`Services/Embedding/`)
   - `EmbeddingService`: Cosine similarity calculations using OpenAI embeddings

6. **Inference Pipeline** (`Services/`)
   - `InferenceService`: Orchestrates the complete pipeline:
     1. Call generator
     2. Run drift evaluation
     3. Route to judge (if needed)
     4. Handle rejections with fallback
     5. Log metrics and return result

## API Endpoints

### POST /api/infer

Process a user message and return intent classification with validation.

**Request:**
```json
{
  "message": "I want to know my payoff amount",
  "sessionId": "optional-session-id"
}
```

**Response:**
```json
{
  "intent": "PayoffQuote",
  "confidence": 0.92,
  "judgeValid": true,
  "driftDetected": false,
  "variantUsed": "gpt4o-mini",
  "judgeSkipped": false,
  "routingStrategy": "sensitive-only"
}
```

### GET /api/health

Health check endpoint.

## Configuration

### appsettings.json

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  },
  "Datadog": {
    "Host": "localhost",
    "Port": "8125"
  },
  "Experiments": {
    "JudgeModel": "gpt4o-mini",
    "RoutingStrategy": "sensitive-only"
  }
}
```

### Environment Variables

- `OpenAI__ApiKey`: Your OpenAI API key
- `Datadog__Host`: Datadog agent hostname
- `Datadog__Port`: Datadog StatsD port (default: 8125)

## Datadog Integration

### Experiments

The system uses two Datadog experiments:

1. **llm-judge-model**: Selects which judge model to use
   - Variants: `gpt4o-mini`, `gpt41-mini`, `gpt4o-strong`

2. **judge-routing-strategy**: Determines when to invoke the judge
   - Variants: `always`, `sensitive-only`, `low-confidence`

### Metrics

The following metrics are tracked:

- `llm.generator.invoked`: Generator invocations
- `llm.generator.latency_ms`: Generator latency
- `llm.judge.accept`: Judge accepted predictions
- `llm.judge.reject`: Judge rejected predictions
- `llm.judge.violation`: Violations detected (tagged by type)
- `llm.judge.latency_ms`: Judge evaluation latency
- `llm.judge.invoked`: Judge invocations
- `llm.judge.skipped`: Judge skipped by router
- `llm.drift.evaluated`: Drift evaluations
- `llm.inference.completed`: Complete inference pipelines
- `llm.inference.error`: Inference errors
- `llm.fallback.triggered`: Fallback triggered due to rejection

### Tags

All metrics include relevant tags:

- `experiment`: Experiment name
- `variant`: Variant selected
- `intent`: Predicted intent
- `judge_model`: Judge model used
- `violation_type`: Type of violation detected
- `drift_detected`: Whether drift was detected
- `routing_strategy`: Routing strategy used

## Running the Application

### Prerequisites

- .NET 8.0 SDK
- OpenAI API key
- Datadog agent (optional, for metrics)

### Steps

1. Clone the repository
2. Update `appsettings.json` with your OpenAI API key
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Run the application:
   ```bash
   dotnet run --project LLMJudgePipeline
   ```
5. Access Swagger UI: `https://localhost:5001/swagger`

## Testing

Example cURL request:

```bash
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I need to make a payment on my loan",
    "sessionId": "test-session-123"
  }'
```

## Project Structure

```
LLMJudgePipeline/
├── Controllers/
│   └── InferenceController.cs
├── Infrastructure/
│   ├── DatadogExperimentsService.cs
│   └── DatadogMetricsService.cs
├── Interfaces/
│   ├── IDatadogExperiments.cs
│   ├── IDatadogMetrics.cs
│   ├── IDriftEvaluator.cs
│   ├── IEmbeddingService.cs
│   ├── IInferenceService.cs
│   ├── IJudgeRouter.cs
│   ├── ILlmGenerator.cs
│   └── ILlmJudge.cs
├── Models/
│   ├── DriftEvalResult.cs
│   ├── GeneratorResult.cs
│   ├── InferenceRequest.cs
│   ├── InferenceResponse.cs
│   └── JudgeResult.cs
├── Services/
│   ├── Drift/
│   │   └── DriftEvaluator.cs
│   ├── Embedding/
│   │   └── EmbeddingService.cs
│   ├── Generators/
│   │   └── Gpt4oGenerator.cs
│   ├── Judges/
│   │   ├── ExperimentAwareJudge.cs
│   │   ├── Gpt4oMiniJudge.cs
│   │   ├── Gpt41MiniJudge.cs
│   │   └── Gpt4oStrongJudge.cs
│   ├── Routing/
│   │   └── RiskBasedJudgeRouter.cs
│   └── InferenceService.cs
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── LLMJudgePipeline.csproj
```

## SOLID Principles

- **Single Responsibility**: Each class has one clear purpose
- **Open/Closed**: Easily extendable with new judge implementations
- **Liskov Substitution**: All judges implement ILlmJudge interface
- **Interface Segregation**: Focused interfaces for each concern
- **Dependency Inversion**: Dependencies injected via interfaces

## Extensibility

### Adding a New Judge

1. Create a new class implementing `ILlmJudge`
2. Register it in `Program.cs`
3. Update `ExperimentAwareJudge` to map the new variant

### Adding a New Routing Strategy

1. Update `RiskBasedJudgeRouter.RouteAsync()` with new logic
2. Add the variant to your Datadog experiment configuration

### Customizing Prompts

All prompts are defined as constants in their respective classes and can be easily modified or externalized to configuration.

## License

MIT
