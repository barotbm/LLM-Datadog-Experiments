using Azure.AI.OpenAI;
using LLMJudgePipeline.Infrastructure;
using LLMJudgePipeline.Interfaces;
using LLMJudgePipeline.Prompts.Extensions;
using LLMJudgePipeline.Services;
using LLMJudgePipeline.Services.Drift;
using LLMJudgePipeline.Services.Embedding;
using LLMJudgePipeline.Services.Generators;
using LLMJudgePipeline.Services.Judges;
using LLMJudgePipeline.Services.Routing;
using Serilog;
using StatsdClient;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LLM Judge Pipeline API", Version = "v1" });
});

// Configure OpenAI Client
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"] 
    ?? throw new InvalidOperationException("OpenAI API Key not configured");

builder.Services.AddSingleton(sp => 
{
    return new OpenAIClient(openAiApiKey);
});

// Configure DogStatsD
var datadogHost = builder.Configuration["Datadog:Host"] ?? "localhost";
var datadogPort = int.Parse(builder.Configuration["Datadog:Port"] ?? "8125");

var dogstatsdConfig = new StatsdConfig
{
    StatsdServerName = datadogHost,
    StatsdPort = datadogPort,
    Prefix = "llm_judge_pipeline"
};

DogStatsd.Configure(dogstatsdConfig);

// Register Datadog services
builder.Services.AddSingleton<IDatadogMetrics, DatadogMetricsService>();
builder.Services.AddSingleton<IDatadogExperiments, DatadogExperimentsService>();

// Register Prompt Registry and versioning system
builder.Services.AddPromptRegistry(builder.Configuration);

// Register Generator
builder.Services.AddScoped<ILlmGenerator, Gpt4oGenerator>();

// Register all Judge implementations
builder.Services.AddScoped<Gpt4oMiniJudge>();
builder.Services.AddScoped<Gpt41MiniJudge>();
builder.Services.AddScoped<Gpt4oStrongJudge>();
builder.Services.AddScoped<ExperimentAwareJudge>();

// Register Embedding Service
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

// Register Drift Evaluator
builder.Services.AddScoped<IDriftEvaluator, DriftEvaluator>();

// Register Judge Router
builder.Services.AddScoped<IJudgeRouter, RiskBasedJudgeRouter>();

// Register Inference Service
builder.Services.AddScoped<IInferenceService, InferenceService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Warmup prompt cache
app.WarmupPromptCache();

Log.Information("LLM Judge Pipeline API starting up...");

app.Run();
