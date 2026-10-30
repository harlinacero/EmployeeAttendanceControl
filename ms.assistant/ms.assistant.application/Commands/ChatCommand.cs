using MediatR;
using ms.assistant.application.Requests;
using ms.assistant.application.Responses;

namespace ms.assistant.application.Commands
{
    public record ChatCommand(string UserName, bool IsAdmin, string Token, string Question, IReadOnlyList<ChatMessageRequest> History) : IRequest<ChatResponse>;
}
