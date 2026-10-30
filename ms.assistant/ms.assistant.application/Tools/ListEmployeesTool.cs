using Microsoft.Extensions.Logging;
using ms.assistant.application.HttpComunications;
using ms.assistant.domain.Entities;
using System.Text.Json;

namespace ms.assistant.application.Tools
{
    public class ListEmployeesTool : AssistantToolBase<ListEmployeesTool>
    {
        private readonly IEmployeeApiCommunication _employeeApi;

        public ListEmployeesTool(IEmployeeApiCommunication employeeApi, ILogger<ListEmployeesTool> logger) : base(logger)
        {
            _employeeApi = employeeApi;
        }

        public override string Name => "list_employees";

        public override string Description =>
            "Returns every employee with their current attendance state: userName, firstName, lastName, " +
            "lastAttendanceState (true = present, false = absent), lastAttendance (date of the last change) and lastAttendanceNotes.";

        public override IReadOnlyDictionary<string, JsonElement> InputSchema => NoInputs;

        public override bool AdminOnly => true;

        public override Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken cancellationToken)
            => RetrieveAsync(() => _employeeApi.GetAllEmployees(context.Token), context);
    }
}
