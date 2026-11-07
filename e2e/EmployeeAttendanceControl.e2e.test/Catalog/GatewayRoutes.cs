namespace EmployeeAttendanceControl.e2e.test.Catalog
{
    /// <summary>
    /// The operations exposed through the gateway. GatewayRoutesTests keeps it in sync with ocelot.json.
    /// </summary>
    public static class GatewayRoutes
    {
        public const string AssistantChat = "POST /Assistant/Chat";

        public static readonly IReadOnlyList<GatewayRoute> All =
        [
            new(HttpMethod.Post, "/Authentication", AccessGroup.Anonymous, "/Authentication", new { userName = "admin", password = "1234" }),
            new(HttpMethod.Post, "/Authentication/Refresh", AccessGroup.Anonymous, "/Authentication/Refresh", new { refreshToken = "access-check" }),
            new(HttpMethod.Post, "/Authentication/Logout", AccessGroup.Anonymous, "/Authentication/Logout", new { refreshToken = "access-check" }),
            new(HttpMethod.Get, "/User", AccessGroup.Admin, "/User"),
            new(HttpMethod.Post, "/User", AccessGroup.Admin, "/User", new { userName = "e2e-access-check", password = "e2e", role = "user" }),
            new(HttpMethod.Get, "/Employees", AccessGroup.Admin, "/Employees"),
            new(HttpMethod.Post, "/Employees", AccessGroup.Admin, "/Employees", new { userName = "e2e-access-check", firstName = "E2E", lastName = "Access", password = "e2e", role = "user" }),
            new(HttpMethod.Put, "/Employees/AttendanceState/{attendance}", AccessGroup.Identified, "/Employees/AttendanceState/true", "access check"),
            new(HttpMethod.Get, "/Attendances", AccessGroup.Identified, "/Attendances?userName=admin"),
            new(HttpMethod.Get, "/Attendances/ByDateRange", AccessGroup.Admin, "/Attendances/ByDateRange?userName=admin&from=2026-01-01&to=2026-01-31"),
            new(HttpMethod.Post, "/Attendances", AccessGroup.Identified, "/Attendances", new { fullName = "E2E Access", date = DateTime.UtcNow, attendance = true, notes = "access check" }),
            new(HttpMethod.Post, "/Assistant/Chat", AccessGroup.Identified, "/Assistant/Chat", new { question = "access check" })
        ];

        public static TheoryData<string> Protected => Operations(route => route.Access != AccessGroup.Anonymous);

        public static TheoryData<string> AdminOnly => Operations(route => route.Access == AccessGroup.Admin);

        // The assistant calls a paid service, so accepting a regular user is checked only by AssistantTests
        public static TheoryData<string> OpenToAnyUser => Operations(route => route.Access == AccessGroup.Identified && route.Operation != AssistantChat);

        public static GatewayRoute Find(string operation) => All.Single(route => route.Operation == operation);

        private static TheoryData<string> Operations(Func<GatewayRoute, bool> filter)
        {
            var data = new TheoryData<string>();
            foreach (var route in All.Where(filter))
            {
                data.Add(route.Operation);
            }

            return data;
        }
    }
}
