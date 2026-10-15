using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ms.employees.application.Commands;
using ms.employees.application.Queries;
using ms.employees.application.Requests;
using System.Security.Claims;

namespace ms.employees.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> Employees()
            => Ok(await _mediator.Send(new GetAllEmployeesQuery()));

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Employee([FromBody] CreateEmployeeRequest request)
            => Ok(await _mediator.Send(new CreateEmployeeCommand(request.UserName, request.FirstName, request.LastName, request.Password, request.Role)));

        [Authorize]
        [HttpPut]
        [Route("[action]/{attendance}")]
        public async Task<IActionResult> AttendanceState(bool attendance, [FromBody] string notes)
        {
            var username = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            return Ok(await _mediator.Send(new UpdateAttendanceStateCommand(username, attendance, notes, Request.Headers["Authorization"].ToString())));
        }


    }
}
