namespace EmployeeAttendanceControl.e2e.test.Support
{
    /// <summary>
    /// The real assistant costs money and isn't deterministic, so its check runs only on request and with a key.
    /// </summary>
    public sealed class AssistantFactAttribute : FactAttribute
    {
        public AssistantFactAttribute()
        {
            Skip = SkipReason(
                Environment.GetEnvironmentVariable("E2E_ASSISTANT"),
                Environment.GetEnvironmentVariable("E2E_ASSISTANT_KEY_PROVIDED"))!;
        }

        public static string? SkipReason(string? requested, string? keyProvided)
        {
            if (requested != "true")
            {
                return "assistant check not requested";
            }

            return keyProvided == "true" ? null : "ANTHROPIC_API_KEY not provided";
        }
    }
}
