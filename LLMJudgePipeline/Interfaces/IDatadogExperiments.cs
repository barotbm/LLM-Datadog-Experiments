namespace LLMJudgePipeline.Interfaces;

public interface IDatadogExperiments
{
    string GetVariant(string experimentName, string sessionId);
}
