using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ms.assistant.application.Extensions;
using ms.assistant.application.Prompts;
using ms.assistant.application.Requests;
using ms.assistant.application.Responses;
using ms.assistant.application.Tools;
using ms.assistant.domain.Entities;
using ms.assistant.domain.Exceptions;
using ms.assistant.domain.Services;

namespace ms.assistant.application.Commands.Handlers
{
    /// <summary>
    /// The agent loop: ask the model, run the tools it requests with the caller's identity, send the results back,
    /// and repeat until the model answers or the round limit is reached.
    /// </summary>
    public class ChatCommandHandler : IRequestHandler<ChatCommand, ChatResponse>
    {
        public const string MaxRoundsAnswer = "I couldn't complete the answer. Please rephrase or narrow the question.";
        public const string RefusalAnswer = "I can't help with that request. I can answer questions about attendance.";
        public const string ToolNotAvailableMessage = "This tool is not available.";
        private const string TruncatedSuffix = " (answer truncated)";

        private readonly IAssistantModel _model;
        private readonly IAssistantToolCatalog _toolCatalog;
        private readonly AssistantOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<ChatCommandHandler> _logger;

        public ChatCommandHandler(IAssistantModel model,
            IAssistantToolCatalog toolCatalog,
            IOptions<AssistantOptions> options,
            TimeProvider timeProvider,
            ILogger<ChatCommandHandler> logger)
        {
            _model = model;
            _toolCatalog = toolCatalog;
            _options = options.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<ChatResponse> Handle(ChatCommand request, CancellationToken cancellationToken)
        {
            // 1. Tools depend on the caller's role; the context comes from the JWT, never from the model.
            var tools = _toolCatalog.For(request.IsAdmin);
            var toolDefinitions = tools.Select(tool => new ToolDefinition(tool.Name, tool.Description, tool.InputSchema, tool.Required)).ToList();
            var context = new ToolContext(request.UserName, request.Token);
            var systemPrompt = AssistantPrompt.Build(request.IsAdmin, DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime));

            // 2. The model has no memory: send the previous messages plus the new question.
            var messages = request.History
                .Select(message => message.Role == ChatMessageRequest.AssistantRole ? ChatMessage.Assistant(message.Content) : ChatMessage.User(message.Content))
                .Append(ChatMessage.User(request.Question))
                .ToList();

            var usedTools = new List<string>();
            int inputTokens = 0, outputTokens = 0, rounds = 0;
            string answer = null;

            // 3. The loop.
            while (answer is null && rounds < _options.MaxToolRounds)
            {
                rounds++;
                ModelReply reply;
                try
                {
                    reply = await _model.SendAsync(systemPrompt, messages.ToList(), toolDefinitions, cancellationToken);
                }
                catch (AssistantModelUnavailableException ex)
                {
                    _logger.LogError(ex, "AI model unavailable while answering {UserName}", request.UserName);
                    throw;
                }

                inputTokens += reply.InputTokens;
                outputTokens += reply.OutputTokens;

                if (reply.StopReason == ModelStopReason.Refusal)
                {
                    answer = RefusalAnswer;
                }
                else if (reply.StopReason == ModelStopReason.MaxTokens)
                {
                    _logger.LogWarning("Answer for {UserName} reached the max tokens limit", request.UserName);
                    answer = reply.Text + TruncatedSuffix;
                }
                else if (reply.ToolCalls.Count == 0)
                {
                    answer = reply.Text;
                }
                else
                {
                    // The model asked for tools: run them and send every result back in one message.
                    messages.Add(reply.AssistantMessage);
                    var results = new List<ToolResult>();
                    foreach (var call in reply.ToolCalls)
                    {
                        usedTools.Add(call.Name);
                        results.Add(await ExecuteToolAsync(call, tools, context, cancellationToken));
                    }
                    messages.Add(ChatMessage.Results(results));
                }
            }

            if (answer is null)
            {
                _logger.LogWarning("Assistant reached {MaxToolRounds} tool rounds without answering {UserName}", _options.MaxToolRounds, request.UserName);
                answer = MaxRoundsAnswer;
            }

            // 4. Usage for support and cost monitoring, without the text of the question or the answer.
            _logger.LogInformation("Assistant answered {UserName} (admin: {IsAdmin}) in {Rounds} rounds; tools {Tools}; input tokens {InputTokens}; output tokens {OutputTokens}",
                request.UserName, request.IsAdmin, rounds, string.Join(",", usedTools), inputTokens, outputTokens);

            return new ChatResponse { Answer = answer };
        }

        private async Task<ToolResult> ExecuteToolAsync(ToolCall call, IReadOnlyList<IAssistantTool> tools, ToolContext context, CancellationToken cancellationToken)
        {
            var tool = tools.FirstOrDefault(candidate => candidate.Name == call.Name);
            if (tool is null)
            {
                _logger.LogWarning("Model requested unavailable tool {Tool} for {UserName}", call.Name, context.UserName);
                return ToolResult.Error(ToolNotAvailableMessage) with { ToolCallId = call.Id };
            }

            var result = await tool.ExecuteAsync(call.Input, context, cancellationToken);
            return result with { ToolCallId = call.Id };
        }
    }
}
