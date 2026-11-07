using MediatR;
using Microsoft.AspNetCore.Mvc;
using ms.users.application.Commands;
using ms.users.application.Request;
using ms.users.domain.Exceptions;

namespace ms.users.api.Controllers
{
    /// <summary>
    /// Anonymous: the credentials, or the refresh token, are the proof. Data unavailable is answered with 503 by DataUnavailableExceptionHandler.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        public const string InvalidCredentialsMessage = "Invalid username or password";
        public const string InvalidRefreshTokenMessage = "Invalid refresh token";

        private readonly IMediator _mediator;

        public AuthenticationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> DoLogin([FromBody] LoginCredentialsRequest loginCredentials)
        {
            try
            {
                return Ok(await _mediator.Send(new LoginCommand(loginCredentials.UserName, loginCredentials.Password)));
            }
            catch (InvalidCredentialsException)
            {
                return Unauthorized(InvalidCredentialsMessage);
            }
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                return Ok(await _mediator.Send(new RefreshSessionCommand(request.RefreshToken)));
            }
            catch (InvalidCredentialsException)
            {
                return Unauthorized(InvalidRefreshTokenMessage);
            }
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            await _mediator.Send(new LogoutCommand(request.RefreshToken));
            return NoContent();
        }
    }
}
