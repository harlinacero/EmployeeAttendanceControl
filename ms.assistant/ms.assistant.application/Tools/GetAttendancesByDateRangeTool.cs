using Microsoft.Extensions.Logging;
using ms.assistant.application.HttpComunications;
using ms.assistant.domain.Entities;
using System.Globalization;
using System.Text.Json;

namespace ms.assistant.application.Tools
{
    public class GetAttendancesByDateRangeTool : AssistantToolBase<GetAttendancesByDateRangeTool>
    {
        // Same rule as GET /Attendances/ByDateRange in ms.attendances (tech spec D-06).
        public const int MaxRangeDays = 31;
        private const string DateFormat = "yyyy-MM-dd";

        private readonly IAttendanceApiCommunication _attendanceApi;

        public GetAttendancesByDateRangeTool(IAttendanceApiCommunication attendanceApi, ILogger<GetAttendancesByDateRangeTool> logger) : base(logger)
        {
            _attendanceApi = attendanceApi;
        }

        public override string Name => "get_attendances_by_date_range";

        public override string Description =>
            "Returns the attendance records of one employee between two dates, both days inclusive, most recent first. " +
            $"The range can be at most {MaxRangeDays} days. Each record has date, attendance (true = present, false = absent) and notes.";

        public override IReadOnlyDictionary<string, JsonElement> InputSchema { get; } = new Dictionary<string, JsonElement>
        {
            ["userName"] = Property("string", "User name of the employee."),
            ["from"] = Property("string", $"First day of the range, format {DateFormat}."),
            ["to"] = Property("string", $"Last day of the range, format {DateFormat}.")
        };

        public override IReadOnlyList<string> Required => ["userName", "from", "to"];

        public override bool AdminOnly => true;

        public override Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken cancellationToken)
        {
            foreach (var field in Required)
            {
                if (!TryGetString(input, field, out _))
                {
                    return Task.FromResult(ToolResult.Error($"Invalid input: {field}"));
                }
            }

            TryGetString(input, "userName", out var userName);
            TryGetString(input, "from", out var fromText);
            TryGetString(input, "to", out var toText);

            if (!TryParseDate(fromText, out var from))
            {
                return Task.FromResult(ToolResult.Error($"Invalid input: from must be a valid date with format {DateFormat}."));
            }
            if (!TryParseDate(toText, out var to))
            {
                return Task.FromResult(ToolResult.Error($"Invalid input: to must be a valid date with format {DateFormat}."));
            }
            if (from > to)
            {
                return Task.FromResult(ToolResult.Error("The start date cannot be later than the end date."));
            }
            if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            {
                return Task.FromResult(ToolResult.Error($"The date range cannot exceed {MaxRangeDays} days."));
            }

            return RetrieveAsync(() => _attendanceApi.GetAttendancesByDateRange(userName, fromText, toText, context.Token), context);
        }

        private static bool TryParseDate(string text, out DateOnly date)
            => DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
