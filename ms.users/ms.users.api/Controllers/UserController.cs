using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ms.users.application.Commands;
using ms.users.application.Queries;
using ms.users.application.Request;

namespace ms.users.api.Controllers
{
    [Authorize(Roles ="admin")]
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers() => Ok(await _mediator.Send(new GetAllUsersQuery()));

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] AccountRequest account) =>
            Ok(await _mediator.Send(new CreateUserAccountCommand(account.UserName, account.Password, account.Role)));
    }
}
