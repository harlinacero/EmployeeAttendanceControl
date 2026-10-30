using Microsoft.Extensions.Logging;
using ms.assistant.application.HttpComunications;
using ms.assistant.domain.Entities;
using System.Text.Json;

namespace ms.assistant.application.Tools
{
    public class GetMyAttendancesTool : AssistantToolBase<GetMyAttendancesTool>
    {
        private readonly IAttendanceApiCommunication _attendanceApi;

        public GetMyAttendancesTool(IAttendanceApiCommunication attendanceApi, ILogger<GetMyAttendancesTool> logger) : base(logger)
        {
            _attendanceApi = attendanceApi;
        }

        public override string Name => "get_my_attendances";

        public override string Description =>
            "Returns the full attendance history of the user who is asking: one record per attendance change, with date, " +
            "attendance (true = present, false = absent) and notes. Takes no input: it always returns the asker's own records.";

        // No userName input on purpose: the user always comes from the caller's token (AC-03).
        public override IReadOnlyDictionary<string, JsonElement> InputSchema => NoInputs;

        public override bool AdminOnly => false;

        public override Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken cancellationToken)
            => RetrieveAsync(() => _attendanceApi.GetAllAttendances(context.UserName, context.Token), context);
    }
}
