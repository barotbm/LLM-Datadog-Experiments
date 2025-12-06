# System Diagrams

## Complete System Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                            CLIENT APPLICATION                               │
│                                                                             │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │
                                │ POST /api/infer
                                │ {
                                │   "message": "I want my payoff amount",
                                │   "sessionId": "abc123"
                                │ }
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         INFERENCE CONTROLLER                                │
│                         • Input validation                                  │
│                         • Request routing                                   │
│                         • Response formatting                               │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         INFERENCE SERVICE                                   │
│                    Pipeline Orchestration Layer                             │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 1: Generate Intent                                             │   │
│  │ ┌────────────────────┐      ┌─────────────────┐                    │   │
│  │ │  Gpt4oGenerator    │─────▶│  OpenAI API     │                    │   │
│  │ │  • GPT-4o          │      │  gpt-4o         │                    │   │
│  │ │  • JSON response   │◀─────│                 │                    │   │
│  │ └────────────────────┘      └─────────────────┘                    │   │
│  │                                                                     │   │
│  │ Result: { intent: "PayoffQuote", confidence: 0.92 }                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                │                                            │
│                                ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 2: Evaluate Drift                                              │   │
│  │ ┌────────────────────┐      ┌─────────────────┐                    │   │
│  │ │  DriftEvaluator    │─────▶│ EmbeddingService│                    │   │
│  │ │                    │      │                 │                    │   │
│  │ │ • Similarity       │      │ OpenAI API      │                    │   │
│  │ │ • Domain check     │◀─────│ ada-002         │                    │   │
│  │ │ • State validation │      │                 │                    │   │
│  │ └────────────────────┘      └─────────────────┘                    │   │
│  │                                                                     │   │
│  │ Result: { driftDetected: false, similarity: 0.88 }                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                │                                            │
│                                ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 3: Route to Judge                                              │   │
│  │ ┌────────────────────┐      ┌─────────────────────────────────┐    │   │
│  │ │ RiskBasedRouter    │      │ Datadog Experiments             │    │   │
│  │ │                    │─────▶│ "judge-routing-strategy"        │    │   │
│  │ │ Should evaluate?   │      │                                 │    │   │
│  │ │                    │◀─────│ Variant: "sensitive-only"       │    │   │
│  │ └────────────────────┘      └─────────────────────────────────┘    │   │
│  │         │                                                           │   │
│  │         │ If YES                                                    │   │
│  │         ▼                                                           │   │
│  │ ┌────────────────────────────────────────────────────────────┐     │   │
│  │ │ ExperimentAwareJudge                                       │     │   │
│  │ │ ┌────────────────┐      ┌────────────────────────────┐    │     │   │
│  │ │ │ Select Judge   │─────▶│ Datadog Experiments        │    │     │   │
│  │ │ │                │      │ "llm-judge-model"          │    │     │   │
│  │ │ │                │◀─────│ Variant: "gpt4o-mini"      │    │     │   │
│  │ │ └────────────────┘      └────────────────────────────┘    │     │   │
│  │ │         │                                                  │     │   │
│  │ │         ▼                                                  │     │   │
│  │ │ ┌─────────────────┐     ┌──────────────────┐             │     │   │
│  │ │ │ Gpt4oMiniJudge  │────▶│   OpenAI API     │             │     │   │
│  │ │ │                 │     │   gpt-4o-mini    │             │     │   │
│  │ │ │ • Evaluate      │◀────│                  │             │     │   │
│  │ │ │ • Check safety  │     └──────────────────┘             │     │   │
│  │ │ │ • Validate      │                                      │     │   │
│  │ │ └─────────────────┘                                      │     │   │
│  │ └────────────────────────────────────────────────────────────┘     │   │
│  │                                                                     │   │
│  │ Result: { isValid: true, violation: "none" }                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                │                                            │
│                                ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 4: Handle Result & Fallback                                   │   │
│  │                                                                     │   │
│  │ IF judge.isValid == false:                                          │   │
│  │    intent = "RequiresReview"                                        │   │
│  │    Trigger fallback logic                                           │   │
│  │                                                                     │   │
│  │ ELSE:                                                               │   │
│  │    intent = generatorResult.intent                                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                │                                            │
│                                ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ STEP 5: Log Metrics & Update History                               │   │
│  │ ┌────────────────────┐      ┌─────────────────┐                    │   │
│  │ │ DatadogMetrics     │─────▶│ Datadog Agent   │                    │   │
│  │ │                    │      │ DogStatsD       │                    │   │
│  │ │ • llm.*.invoked    │      │ localhost:8125  │                    │   │
│  │ │ • llm.*.latency_ms │      │                 │                    │   │
│  │ │ • Tagged metrics   │      │                 │                    │   │
│  │ └────────────────────┘      └─────────────────┘                    │   │
│  │                                                                     │   │
│  │ Update conversation history for session                             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                │                                            │
└────────────────────────────────┼────────────────────────────────────────────┘
                                 │
                                 ▼
                        ┌─────────────────┐
                        │   RESPONSE      │
                        │                 │
                        │ {               │
                        │   intent,       │
                        │   confidence,   │
                        │   judgeValid,   │
                        │   driftDetected,│
                        │   variantUsed   │
                        │ }               │
                        └─────────────────┘
```

## Judge Selection Decision Tree

```
                        Start Request
                              │
                              ▼
                    ┌──────────────────┐
                    │ Call Generator   │
                    │ (GPT-4o)         │
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │ Get Routing      │
                    │ Strategy         │
                    │ (Experiment)     │
                    └──────────────────┘
                              │
                ┌─────────────┼─────────────┐
                │             │             │
                ▼             ▼             ▼
         ┌──────────┐  ┌─────────────┐  ┌──────────────┐
         │ always   │  │ sensitive-  │  │ low-         │
         │          │  │ only        │  │ confidence   │
         └──────────┘  └─────────────┘  └──────────────┘
                │             │                  │
                │             ▼                  ▼
                │      ┌─────────────┐    ┌──────────────┐
                │      │ Is intent   │    │ Confidence   │
                │      │ sensitive?  │    │ < 0.75?      │
                │      └─────────────┘    └──────────────┘
                │         │      │           │        │
                │        YES    NO          YES      NO
                │         │      │           │        │
                └─────────┴──────┘           │        │
                          │                  │        │
                         YES                YES      NO
                          │                  │        │
                          ▼                  ▼        ▼
                    ┌──────────────────┐         ┌─────────┐
                    │ Get Judge Model  │         │ Skip    │
                    │ (Experiment)     │         │ Judge   │
                    └──────────────────┘         └─────────┘
                              │                        │
            ┌─────────────────┼─────────────┐         │
            │                 │             │         │
            ▼                 ▼             ▼         │
     ┌──────────┐      ┌──────────┐  ┌──────────┐   │
     │gpt4o-mini│      │gpt41-mini│  │gpt4o-    │   │
     │          │      │          │  │strong    │   │
     └──────────┘      └──────────┘  └──────────┘   │
            │                 │             │        │
            └─────────────────┼─────────────┘        │
                              ▼                      ▼
                    ┌──────────────────┐    ┌──────────────┐
                    │ Evaluate with    │    │ Synthetic    │
                    │ Selected Judge   │    │ Pass Result  │
                    └──────────────────┘    └──────────────┘
                              │                      │
                              └──────────┬───────────┘
                                         ▼
                                  ┌─────────────┐
                                  │ Return      │
                                  │ Result      │
                                  └─────────────┘
```

## Metrics Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    Application Layer                            │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │  Generator   │  │    Judge     │  │    Drift     │         │
│  │              │  │              │  │              │         │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘         │
│         │                 │                 │                 │
│         │ Increment       │ Increment       │ Increment       │
│         │ Histogram       │ Histogram       │                 │
│         │                 │                 │                 │
└─────────┼─────────────────┼─────────────────┼─────────────────┘
          │                 │                 │
          │                 │                 │
          ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    DatadogMetricsService                        │
│                                                                 │
│  • Increment(metric, value, tags)                              │
│  • Histogram(metric, value, tags)                              │
│  • Gauge(metric, value, tags)                                  │
│                                                                 │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            │ DogStatsD Protocol
                            │ UDP localhost:8125
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Datadog Agent                              │
│                                                                 │
│  • Receives metrics via DogStatsD                              │
│  • Aggregates and buffers                                      │
│  • Forwards to Datadog platform                                │
│                                                                 │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            │ HTTPS
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Datadog Platform                            │
│                                                                 │
│  • Stores metrics                                              │
│  • Enables dashboards                                          │
│  • Powers alerts                                               │
│  • Provides analytics                                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

Metrics with Tags:
─────────────────
llm.generator.invoked
  ├─ intent:PayoffQuote
  └─ confidence_bucket:high

llm.judge.latency_ms
  ├─ experiment:llm-judge-model
  ├─ variant:gpt4o-mini
  ├─ intent:PayoffQuote
  └─ judge_model:gpt4o-mini

llm.inference.completed
  ├─ experiment:llm-judge-model
  ├─ variant:gpt4o-mini
  ├─ routing_strategy:sensitive-only
  ├─ intent:PayoffQuote
  ├─ drift_detected:false
  └─ judge_valid:true
```

## Class Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         «interface»                             │
│                        ILlmGenerator                            │
│  + GenerateAsync(message, prompt): Task<GeneratorResult>       │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ implements
                             ▼
                    ┌─────────────────┐
                    │ Gpt4oGenerator  │
                    │                 │
                    │ - _openAiClient │
                    │ - _logger       │
                    └─────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         «interface»                             │
│                          ILlmJudge                              │
│  + EvaluateAsync(message, output): Task<JudgeResult>           │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ implements
          ┌──────────────────┼──────────────────┐
          │                  │                  │
          ▼                  ▼                  ▼
  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
  │Gpt4oMiniJudge│  │Gpt41MiniJudge│  │Gpt4oStrong   │
  │              │  │              │  │Judge         │
  └──────────────┘  └──────────────┘  └──────────────┘
          │                  │                  │
          └──────────────────┼──────────────────┘
                             │
                             │ uses
                             ▼
                ┌─────────────────────────┐
                │ ExperimentAwareJudge    │
                │                         │
                │ - _experiments          │
                │ - _metrics              │
                │ - _serviceProvider      │
                │                         │
                │ + EvaluateAsync()       │
                └─────────────────────────┘
                             │
                             │ uses
                             ▼
                ┌─────────────────────────┐
                │ «interface»             │
                │ IDatadogExperiments     │
                │                         │
                │ + GetVariant()          │
                └─────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         «interface»                             │
│                        IJudgeRouter                             │
│  + RouteAsync(message, output, sessionId): Task<JudgeResult>   │
│  + ShouldEvaluate(output): bool                                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ implements
                             ▼
                ┌─────────────────────────┐
                │ RiskBasedJudgeRouter    │
                │                         │
                │ - _experimentAwareJudge │
                │ - _experiments          │
                │ - _metrics              │
                │                         │
                │ + RouteAsync()          │
                │ + ShouldEvaluate()      │
                └─────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                         «interface»                             │
│                       IInferenceService                         │
│  + ProcessAsync(request): Task<InferenceResponse>              │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             │ implements
                             ▼
                    ┌─────────────────┐
                    │InferenceService │
                    │                 │
                    │ - _generator    │
                    │ - _driftEval    │
                    │ - _judgeRouter  │
                    │ - _metrics      │
                    │                 │
                    │ + ProcessAsync()│
                    └─────────────────┘
```

## Deployment Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                         Load Balancer                            │
│                    (Azure/AWS/GCP LB)                            │
└────────────────────────┬─────────────────────────────────────────┘
                         │
          ┌──────────────┼──────────────┐
          │              │              │
          ▼              ▼              ▼
    ┌─────────┐    ┌─────────┐    ┌─────────┐
    │Instance1│    │Instance2│    │Instance3│
    │         │    │         │    │         │
    │ ASP.NET │    │ ASP.NET │    │ ASP.NET │
    │  Core   │    │  Core   │    │  Core   │
    └────┬────┘    └────┬────┘    └────┬────┘
         │              │              │
         │              │              │
         └──────────────┼──────────────┘
                        │
          ┌─────────────┼─────────────┐
          │             │             │
          ▼             ▼             ▼
    ┌──────────┐  ┌──────────┐  ┌──────────┐
    │ OpenAI   │  │ Datadog  │  │  Redis   │
    │   API    │  │  Agent   │  │  Cache   │
    │          │  │          │  │ (future) │
    └──────────┘  └──────────┘  └──────────┘
```
