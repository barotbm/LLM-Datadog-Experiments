namespace LLMJudgePipeline.Interfaces;

public interface IDatadogMetrics
{
    void Increment(string metricName, double value = 1, string[]? tags = null);
    void Gauge(string metricName, double value, string[]? tags = null);
    void Histogram(string metricName, double value, string[]? tags = null);
}
