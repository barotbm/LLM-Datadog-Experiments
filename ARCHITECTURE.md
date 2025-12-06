# Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         LLM Judge Pipeline API                          │
│                         POST /api/infer                                 │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         InferenceService                                │
│  Orchestrates the complete pipeline with logging and metrics            │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼───────────────────────────┐
        ▼                           ▼                           ▼
┌───────────────┐          ┌──────────────┐          ┌──────────────────┐
│ GPT-4o        │          │ Drift        │          │ Risk-Based       │
│ Generator     │          │ Evaluator    │          │ Judge Router     │
│               │          │              │          │                  │
│ • Intent      │          │ • Embedding  │          │ • Routing Logic  │
│ • Confidence  │          │   Similarity │          │ • Experiment     │
│ • Entities    │          │ • Domain     │          │   Driven         │
└───────────────┘          │   Check      │          └──────────────────┘
                           │ • State      │                    │
                           │   Validation │                    ▼
                           └──────────────┘          ┌──────────────────┐
                                                     │ Experiment-Aware │
                                                     │ Judge            │
                                                     │                  │
                                                     │ Selects:         │
                                                     │ • gpt4o-mini     │
                                                     │ • gpt41-mini     │
                                                     │ • gpt4o-strong   │
                                                     └──────────────────┘
                                                              │
                                                              ▼
                                                     ┌──────────────────┐
                                                     │ Judge Result     │
                                                     │                  │
                                                     │ • is_valid       │
                                                     │ • violation      │
                                                     │ • confidence     │
                                                     └──────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      Datadog Integration Layer                          │
│  • Experiments/Feature Flags (variant selection)                        │
│  • Metrics (DogStatsD - counters, histograms, gauges)                  │
│  • Structured Logging (tagged with experiment metadata)                │
└─────────────────────────────────────────────────────────────────────────┘
```

## Component Interaction Flow

### 1. Request Flow
```
User Request → InferenceController → InferenceService
```

### 2. Generation Phase
```
InferenceService → Gpt4oGenerator → OpenAI API (gpt-4o)
    ↓
GeneratorResult {
    intent: "PayoffQuote",
    confidence: 0.92,
    entities: {}
}
```

### 3. Drift Evaluation Phase
```
InferenceService → DriftEvaluator
    ↓
DriftEvaluator → EmbeddingService → OpenAI API (text-embedding-ada-002)
    ↓
DriftEvalResult {
    driftDetected: false,
    similarity: 0.88,
    domainMismatch: false,
    invalidStateTransition: false
}
```

### 4. Judge Routing Phase
```
InferenceService → RiskBasedJudgeRouter
    ↓
Router checks Datadog experiment: "judge-routing-strategy"
    Variants: "always", "sensitive-only", "low-confidence"
    ↓
If should evaluate:
    Router → ExperimentAwareJudge
        ↓
    ExperimentAwareJudge checks experiment: "llm-judge-model"
        Variants: "gpt4o-mini", "gpt41-mini", "gpt4o-strong"
        ↓
    Selected Judge → OpenAI API
        ↓
    JudgeResult {
        isValid: true,
        violation: "none",
        confidence: 0.95
    }
Else:
    Return synthetic passing JudgeResult (skipped)
```

### 5. Response Phase
```
InferenceService → Build InferenceResponse
    ↓
Log metrics to Datadog
    ↓
Return response to client
```

## Data Flow

### Input
```json
{
  "message": "I want to know my payoff amount",
  "sessionId": "session-123"
}
```

### Internal Processing

**Step 1: Generator Output**
```json
{
  "intent": "PayoffQuote",
  "confidence": 0.92,
  "rawJson": "{...}",
  "entities": {},
  "timestamp": "2023-12-05T10:30:00Z",
  "tokensUsed": 45,
  "latencyMs": 234
}
```

**Step 2: Drift Evaluation**
```json
{
  "driftDetected": false,
  "similarity": 0.88,
  "domainMismatch": false,
  "invalidStateTransition": false,
  "details": "No drift detected"
}
```

**Step 3: Judge Evaluation**
```json
{
  "isValid": true,
  "violation": "none",
  "confidence": 0.95,
  "judgeModel": "gpt4o-mini",
  "latencyMs": 187,
  "wasSkipped": false
}
```

### Output
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

## Experiment-Driven Decision Making

### Experiment 1: llm-judge-model
Controls which judge model evaluates the output.

**Variants:**
- `gpt4o-mini` (33% traffic) - Fast, cost-effective
- `gpt41-mini` (33% traffic) - Balanced performance
- `gpt4o-strong` (34% traffic) - High accuracy

**Impact:**
- Cost: gpt4o-mini < gpt41-mini < gpt4o-strong
- Latency: gpt4o-mini < gpt41-mini < gpt4o-strong
- Accuracy: gpt4o-mini < gpt41-mini < gpt4o-strong

### Experiment 2: judge-routing-strategy
Controls when the judge is invoked.

**Variants:**
- `always` (33%) - Judge every request
- `sensitive-only` (33%) - Judge only PayoffQuote, Payment, AddressChange
- `low-confidence` (34%) - Judge when generator confidence < 0.75

**Impact:**
- Judge invocation rate: always > sensitive-only > low-confidence
- System latency: always > sensitive-only > low-confidence
- Cost: always > sensitive-only > low-confidence
- Risk coverage: always > sensitive-only ≈ low-confidence

## Metrics Tracked

### Generator Metrics
- `llm.generator.invoked` - Count of generator invocations
- `llm.generator.latency_ms` - Generator response time
  - Tags: `intent`, `confidence_bucket`

### Judge Metrics
- `llm.judge.accept` - Count of accepted evaluations
- `llm.judge.reject` - Count of rejected evaluations
- `llm.judge.violation` - Count of violations by type
  - Tags: `violation_type`
- `llm.judge.latency_ms` - Judge evaluation time
- `llm.judge.invoked` - Count of judge invocations
- `llm.judge.skipped` - Count of skipped evaluations
  - Tags: `experiment`, `variant`, `intent`, `judge_model`, `routing_strategy`

### Drift Metrics
- `llm.drift.evaluated` - Count of drift evaluations
  - Tags: `drift_detected`, `intent`

### Pipeline Metrics
- `llm.inference.completed` - Count of completed inferences
- `llm.inference.error` - Count of errors
- `llm.fallback.triggered` - Count of fallback triggers
  - Tags: `experiment`, `variant`, `routing_strategy`, `intent`, `drift_detected`, `judge_valid`

## Clean Architecture Principles

### Layers

**1. Presentation Layer (Controllers/)**
- `InferenceController` - HTTP API endpoints
- Handles request validation and response formatting

**2. Application Layer (Services/)**
- `InferenceService` - Orchestrates business logic
- Implements use cases and workflows

**3. Domain Layer (Models/)**
- `GeneratorResult`, `JudgeResult`, `DriftEvalResult`
- Core business entities

**4. Infrastructure Layer (Infrastructure/)**
- `DatadogMetricsService` - External metrics service
- `DatadogExperimentsService` - Feature flag service
- External integrations and cross-cutting concerns

### Dependency Flow
```
Controllers → Services → Infrastructure
     ↓           ↓
  Interfaces ← Interfaces
     ↓           ↓
  Models ←── Models
```

### SOLID Adherence

**Single Responsibility**
- Each service has one clear purpose
- Generator only generates, Judge only evaluates

**Open/Closed**
- Easy to add new judge implementations
- New routing strategies can be added without modifying existing code

**Liskov Substitution**
- All judges implement ILlmJudge and are interchangeable
- Routing strategies can be swapped transparently

**Interface Segregation**
- Focused interfaces: ILlmGenerator, ILlmJudge, IDriftEvaluator
- Clients depend only on methods they use

**Dependency Inversion**
- High-level modules depend on abstractions (interfaces)
- Concrete implementations injected via DI container

## Extensibility Points

### Adding a New Judge
1. Create class implementing `ILlmJudge`
2. Register in `Program.cs` DI container
3. Add variant mapping in `ExperimentAwareJudge`

### Adding a New Routing Strategy
1. Add new case to `RiskBasedJudgeRouter.RouteAsync()`
2. Define logic for when to evaluate
3. Configure variant in Datadog experiments

### Adding a New Generator
1. Create class implementing `ILlmGenerator`
2. Register in DI container
3. Update `InferenceService` to use new generator

### Adding Custom Metrics
1. Call `_metrics.Increment()`, `_metrics.Gauge()`, or `_metrics.Histogram()`
2. Add appropriate tags
3. Metrics automatically flow to Datadog

## Security Considerations

### API Key Management
- Never hardcode API keys
- Use user secrets or environment variables
- Rotate keys regularly

### Input Validation
- Validate all user inputs
- Sanitize messages before processing
- Implement rate limiting

### Data Privacy
- Do not log sensitive user data
- Comply with data retention policies
- Use Datadog's data scrubbing features

## Performance Characteristics

### Typical Latencies
- Generator: 200-500ms
- Embedding: 50-150ms
- Judge (mini): 150-300ms
- Judge (strong): 300-600ms
- Total (with judge): 500-1200ms
- Total (judge skipped): 250-650ms

### Throughput
- Single instance: ~5-10 requests/second
- With caching: ~20-50 requests/second
- Horizontal scaling: Linear improvement

### Cost Optimization
- Use `low-confidence` routing to reduce judge calls by ~60%
- Cache embeddings for repeated messages
- Batch requests when possible
- Monitor token usage with Datadog metrics
