# Setup and Deployment Guide

## Prerequisites

### Required Software
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

### Required Accounts & Keys
- OpenAI API Key ([Get one here](https://platform.openai.com/api-keys))
- Datadog Account (optional, for metrics) ([Sign up](https://www.datadoghq.com/))

## Initial Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd LLM-Datadog-Experiments
```

### 2. Configure OpenAI API Key

#### Option A: Update appsettings.json
Edit `LLMJudgePipeline/appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-actual-api-key-here"
  }
}
```

#### Option B: Use User Secrets (Recommended for Development)
```bash
cd LLMJudgePipeline
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-actual-api-key-here"
```

#### Option C: Use Environment Variables
```bash
# Windows PowerShell
$env:OpenAI__ApiKey = "sk-your-actual-api-key-here"

# Windows Command Prompt
set OpenAI__ApiKey=sk-your-actual-api-key-here

# Linux/Mac
export OpenAI__ApiKey="sk-your-actual-api-key-here"
```

### 3. Install Dependencies
```bash
cd LLMJudgePipeline
dotnet restore
```

### 4. Build the Project
```bash
dotnet build
```

## Running the Application

### Development Mode
```bash
cd LLMJudgePipeline
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

### Watch Mode (Auto-reload on changes)
```bash
dotnet watch run
```

## Datadog Agent Setup (Optional)

### Install Datadog Agent

#### Windows
1. Download the [Datadog Agent installer](https://app.datadoghq.com/account/settings#agent/windows)
2. Run the installer
3. Configure with your API key

#### Linux
```bash
DD_API_KEY=<your-api-key> DD_SITE="datadoghq.com" bash -c "$(curl -L https://s3.amazonaws.com/dd-agent/scripts/install_script_agent7.sh)"
```

#### macOS
```bash
DD_API_KEY=<your-api-key> DD_SITE="datadoghq.com" bash -c "$(curl -L https://s3.amazonaws.com/dd-agent/scripts/install_mac_os.sh)"
```

### Configure DogStatsD

The application is configured to send metrics to `localhost:8125` by default. Update `appsettings.json` if your Datadog agent is on a different host:

```json
{
  "Datadog": {
    "Host": "your-datadog-agent-host",
    "Port": "8125"
  }
}
```

### Verify Metrics in Datadog

1. Log in to your Datadog account
2. Navigate to Metrics → Explorer
3. Search for metrics starting with `llm_judge_pipeline.*`

## Configuring Experiments

### Mock Implementation (Default)

The project includes a mock implementation of Datadog experiments that uses simple hashing for variant distribution. Configuration is in `appsettings.json`:

```json
{
  "Experiments": {
    "JudgeModel": "gpt4o-mini",
    "RoutingStrategy": "sensitive-only"
  }
}
```

### Production Implementation

To use actual Datadog Feature Flags:

1. Install the Datadog Feature Flags SDK:
```bash
dotnet add package Datadog.FeatureFlags
```

2. Update `DatadogExperimentsService.cs` to use the actual SDK:
```csharp
// Replace mock implementation with:
var client = new FeatureFlagClient(apiKey);
var variant = client.GetVariation(experimentName, sessionId, defaultValue);
return variant;
```

3. Configure in Datadog UI:
   - Go to Feature Flags in Datadog
   - Create experiments: `llm-judge-model` and `judge-routing-strategy`
   - Define variants and distribution

## Testing the API

### Using Swagger UI
1. Navigate to `https://localhost:5001/swagger`
2. Click on "POST /api/infer"
3. Click "Try it out"
4. Enter a test payload:
```json
{
  "message": "I want to know my payoff amount",
  "sessionId": "test-session-001"
}
```
5. Click "Execute"

### Using cURL
```bash
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I want to know my payoff amount",
    "sessionId": "test-session-001"
  }'
```

### Using PowerShell
```powershell
$body = @{
    message = "I want to know my payoff amount"
    sessionId = "test-session-001"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:5001/api/infer" -Method Post -Body $body -ContentType "application/json"
```

## Deployment

### Docker Deployment

Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LLMJudgePipeline/LLMJudgePipeline.csproj", "LLMJudgePipeline/"]
RUN dotnet restore "LLMJudgePipeline/LLMJudgePipeline.csproj"
COPY . .
WORKDIR "/src/LLMJudgePipeline"
RUN dotnet build "LLMJudgePipeline.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LLMJudgePipeline.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LLMJudgePipeline.dll"]
```

Build and run:
```bash
docker build -t llm-judge-pipeline .
docker run -p 8080:80 -e OpenAI__ApiKey="your-key" llm-judge-pipeline
```

### Azure App Service

1. Publish the app:
```bash
dotnet publish -c Release -o ./publish
```

2. Deploy to Azure:
```bash
az webapp up --name llm-judge-pipeline --resource-group MyResourceGroup
```

3. Configure app settings in Azure Portal:
   - `OpenAI__ApiKey`: Your OpenAI API key
   - `Datadog__Host`: Datadog agent endpoint

### AWS Elastic Beanstalk

1. Install AWS Elastic Beanstalk CLI
2. Initialize:
```bash
eb init -p "64bit Amazon Linux 2 v2.3.0 running .NET Core" llm-judge-pipeline
```

3. Create environment and deploy:
```bash
eb create llm-judge-env
eb deploy
```

## Monitoring and Observability

### Application Logs

Logs are written to console with structured format. View in real-time:
```bash
dotnet run | grep "ERROR\|WARNING"
```

### Datadog Metrics

Monitor these key metrics in Datadog:

- **Generator Performance**
  - `llm_judge_pipeline.llm.generator.latency_ms`
  - `llm_judge_pipeline.llm.generator.invoked`

- **Judge Performance**
  - `llm_judge_pipeline.llm.judge.latency_ms`
  - `llm_judge_pipeline.llm.judge.accept`
  - `llm_judge_pipeline.llm.judge.reject`
  - `llm_judge_pipeline.llm.judge.violation`

- **Drift Detection**
  - `llm_judge_pipeline.llm.drift.evaluated`

- **Pipeline Health**
  - `llm_judge_pipeline.llm.inference.completed`
  - `llm_judge_pipeline.llm.inference.error`

### Creating Datadog Dashboards

1. Go to Dashboards → New Dashboard
2. Add widgets for key metrics
3. Group by tags: `variant`, `intent`, `routing_strategy`

Example query:
```
avg:llm_judge_pipeline.llm.judge.latency_ms{*} by {variant}
```

## Troubleshooting

### OpenAI API Issues

**Problem**: 401 Unauthorized
- **Solution**: Verify your API key is correct and has not expired

**Problem**: 429 Rate Limit Exceeded
- **Solution**: Implement rate limiting or upgrade your OpenAI plan

### Datadog Metrics Not Appearing

**Problem**: Metrics not showing in Datadog
- **Solution**: Verify Datadog agent is running: `sudo systemctl status datadog-agent`
- **Solution**: Check agent logs: `/var/log/datadog/agent.log`
- **Solution**: Verify host and port configuration in appsettings.json

### Application Crashes

**Problem**: Application crashes on startup
- **Solution**: Check logs for missing dependencies
- **Solution**: Verify .NET 8.0 SDK is installed
- **Solution**: Ensure all NuGet packages are restored

### HTTPS Certificate Errors

**Problem**: SSL certificate validation errors in development
- **Solution**: Trust the development certificate:
```bash
dotnet dev-certs https --trust
```

## Performance Tuning

### Optimize for Latency
- Use `gpt4o-mini` judge variant
- Set routing strategy to `sensitive-only` or `low-confidence`
- Cache embeddings for frequently seen messages

### Optimize for Cost
- Use routing strategy `low-confidence` to minimize judge calls
- Implement request caching for identical messages
- Monitor and optimize token usage

### Optimize for Accuracy
- Use `gpt4o-strong` judge variant
- Set routing strategy to `always`
- Lower confidence threshold to 0.85

## Security Best Practices

1. **Never commit API keys** to source control
2. **Use user secrets** or environment variables for sensitive configuration
3. **Enable HTTPS** in production
4. **Implement rate limiting** to prevent abuse
5. **Validate and sanitize** all user inputs
6. **Use Azure Key Vault** or AWS Secrets Manager for production secrets

## Next Steps

1. Implement unit tests
2. Add integration tests
3. Set up CI/CD pipeline
4. Implement request caching
5. Add authentication/authorization
6. Create Datadog alerts for anomalies
7. Implement circuit breakers for OpenAI API calls
8. Add distributed tracing with Datadog APM
