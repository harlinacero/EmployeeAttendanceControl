using Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ms.assistant.domain.Services;
using ms.assistant.infraestucture.AI;

namespace ms.assistant.infraestucture.Extensions
{
    public static class AssistantModelExtension
    {
        public static IServiceCollection AddAssistantModel(this IServiceCollection services)
        {
            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AnthropicOptions>>().Value;
                return new AnthropicClient
                {
                    ApiKey = options.ApiKey,
                    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
                };
            });
            services.AddSingleton<IAssistantModel, ClaudeAssistantModel>();

            return services;
        }
    }
}
