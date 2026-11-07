using MediatR;
using Microsoft.AspNetCore.Mvc;
using ms.users.application.Queries;

namespace ms.users.api.Controllers
{
    /// <summary>
    /// Publishes what other services need to validate tokens: the issuer and the public key. Not routed through the gateway.
    /// </summary>
    [ApiController]
    [Route(".well-known")]
    public class WellKnownController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WellKnownController(IMediator mediator) => _mediator = mediator;

        [HttpGet("openid-configuration")]
        public async Task<IActionResult> OpenIdConfiguration() =>
            Ok(await _mediator.Send(new GetOpenIdConfigurationQuery($"{Request.Scheme}://{Request.Host}/.well-known/jwks.json")));

        [HttpGet("jwks.json")]
        public async Task<IActionResult> Jwks() => Ok(await _mediator.Send(new GetJwksQuery()));
    }
}
