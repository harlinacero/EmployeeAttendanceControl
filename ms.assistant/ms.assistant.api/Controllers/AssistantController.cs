using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ms.assistant.application.Commands;
using ms.assistant.application.Requests;
using System.Security.Claims;

namespace ms.assistant.api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AssistantController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AssistantController(IMediator mediator) => _mediator = mediator;

        [HttpPost("Chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var userName = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            return Ok(await _mediator.Send(new ChatCommand(userName!, User.IsInRole("admin"),
                Request.Headers.Authorization.ToString(), request.Question, request.History ?? [])));
        }
    }
}
