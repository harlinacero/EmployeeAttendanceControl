using Anthropic;
using Anthropic.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ms.assistant.domain.Entities;
using ms.assistant.domain.Exceptions;
using ms.assistant.domain.Services;
using ms.assistant.infraestucture.Extensions;

namespace ms.assistant.infraestucture.AI
{
    /// <summary>
    /// <see cref="IAssistantModel"/> backed by Claude through the official Anthropic SDK.
    /// </summary>
    public class ClaudeAssistantModel : IAssistantModel
    {
        private readonly AnthropicClient _client;
        private readonly AnthropicOptions _options;
        private readonly ILogger<ClaudeAssistantModel> _logger;

        public ClaudeAssistantModel(AnthropicClient client, IOptions<AnthropicOptions> options, ILogger<ClaudeAssistantModel> logger)
        {
            _client = client;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ModelReply> SendAsync(string systemPrompt, IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools, CancellationToken cancellationToken)
        {
            var parameters = ClaudeMessageMapper.ToParams(_options, systemPrompt, messages, tools);
            try
            {
                var response = await _client.Messages.Create(parameters, cancellationToken);
                return ClaudeMessageMapper.ToReply(response);
            }
            catch (Exception ex) when (ex is AnthropicRateLimitException or Anthropic5xxException or AnthropicIOException
                || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                _logger.LogError(ex, "Claude is unavailable ({ExceptionType})", ex.GetType().Name);
                throw new AssistantModelUnavailableException("The AI model is temporarily unavailable.", ex);
            }
            catch (AnthropicApiException ex)
            {
                _logger.LogError(ex, "Claude rejected the request ({ExceptionType}); check the Anthropic configuration", ex.GetType().Name);
                throw;
            }
        }
    }
}
