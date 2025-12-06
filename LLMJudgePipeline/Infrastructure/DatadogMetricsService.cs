using LLMJudgePipeline.Interfaces;
using StatsdClient;

namespace LLMJudgePipeline.Infrastructure;

public class DatadogMetricsService : IDatadogMetrics
{
    private readonly ILogger<DatadogMetricsService> _logger;

    public DatadogMetricsService(ILogger<DatadogMetricsService> logger)
    {
        _logger = logger;
    }

    public void Increment(string metricName, double value = 1, string[]? tags = null)
    {
        try
        {
            DogStatsd.Increment(metricName, (int)value, tags: tags);
            _logger.LogDebug("Metric incremented: {MetricName} = {Value}, Tags: {Tags}", 
                metricName, value, tags != null ? string.Join(", ", tags) : "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing metric: {MetricName}", metricName);
        }
    }

    public void Gauge(string metricName, double value, string[]? tags = null)
    {
        try
        {
            DogStatsd.Gauge(metricName, value, tags: tags);
            _logger.LogDebug("Metric gauge: {MetricName} = {Value}, Tags: {Tags}", 
                metricName, value, tags != null ? string.Join(", ", tags) : "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting gauge metric: {MetricName}", metricName);
        }
    }

    public void Histogram(string metricName, double value, string[]? tags = null)
    {
        try
        {
            DogStatsd.Histogram(metricName, value, tags: tags);
            _logger.LogDebug("Metric histogram: {MetricName} = {Value}, Tags: {Tags}", 
                metricName, value, tags != null ? string.Join(", ", tags) : "none");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording histogram metric: {MetricName}", metricName);
        }
    }
}
