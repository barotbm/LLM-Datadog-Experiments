using LLMJudgePipeline.Prompts.Models;
using LLMJudgePipeline.Prompts.Registry;
using LLMJudgePipeline.Prompts.Validation;

namespace LLMJudgePipeline.Prompts.Extensions;

/// <summary>
/// Extension methods for registering prompt services
/// </summary>
public static class PromptServiceExtensions
{
    /// <summary>
    /// Adds prompt registry and related services to the DI container
    /// </summary>
    public static IServiceCollection AddPromptRegistry(this IServiceCollection services, IConfiguration configuration)
    {
        // Register validator
        services.AddSingleton<PromptSchemaValidator>();

        // Register registry as singleton for caching
        services.AddSingleton<IPromptRegistry, PromptRegistry>();

        // Register configuration
        services.Configure<PromptVersionConfiguration>(
            configuration.GetSection("PromptVersions"));

        return services;
    }

    /// <summary>
    /// Warms up the prompt cache with configured versions
    /// </summary>
    public static IApplicationBuilder WarmupPromptCache(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IPromptRegistry>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var promptVersionsSection = configuration.GetSection("PromptVersions");
        if (promptVersionsSection.Exists())
        {
            var promptVersions = new Dictionary<string, string>();
            
            foreach (var child in promptVersionsSection.GetChildren())
            {
                var value = child.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    // Convert PascalCase config keys to kebab-case prompt names
                    var promptName = ConvertToKebabCase(child.Key);
                    promptVersions[promptName] = value;
                }
            }

            if (promptVersions.Count > 0)
            {
                registry.Warmup(promptVersions);
            }
        }

        return app;
    }

    private static string ConvertToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return string.Concat(
            input.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "-" + char.ToLower(c)
                    : char.ToLower(c).ToString()
            )
        );
    }
}
