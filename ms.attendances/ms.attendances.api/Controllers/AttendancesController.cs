using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ms.attendances.application.Commands;
using ms.attendances.application.Queries;
using ms.attendances.application.Request;
using System.Security.Claims;

namespace ms.attendances.api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AttendancesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AttendancesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> Attendances(string userName) 
            =>  Ok(await _mediator.Send(new GetAllAttendancesQuery(userName)));

        [HttpGet("ByDateRange")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AttendancesByDateRange([FromQuery] GetAttendancesByDateRangeRequest request)
            => Ok(await _mediator.Send(new GetAttendancesByDateRangeQuery(request.UserName, request.From!.Value, request.To!.Value)));

        [HttpPost]
        public async Task<IActionResult> Attendance([FromBody] CreateAttendanceRequest createAttendanceRequest)
        {
            var userName = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            return Ok(await _mediator.Send(new CreateAttendanceCommand(userName, createAttendanceRequest)));
        }
    }
}
