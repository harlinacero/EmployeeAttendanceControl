using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ms.users.application.Commands;
using ms.users.application.Queries;
using ms.users.application.Request;
using ms.users.domain.Exceptions;

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
        public async Task<IActionResult> Users() => Ok(await _mediator.Send(new GetAllUsersQuery()));

        [HttpPost]
        public async Task<IActionResult> Account([FromBody] AccountRequest account)
        {
            try
            {
                return Ok(await _mediator.Send(new CreateUserAccountCommand(account.UserName, account.Password, account.Role)));
            }
            catch (UserAlreadyExistsException exception)
            {
                return Conflict(exception.Message);
            }
        }
    }
}
