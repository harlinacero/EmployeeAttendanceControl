namespace EmployeeAttendanceControl.e2e.test.Catalog
{
    /// <summary>
    /// One operation exposed through the gateway, with a minimal valid request for it.
    /// </summary>
    public record GatewayRoute(HttpMethod Method, string PathTemplate, AccessGroup Access, string SamplePath, object? SampleBody = null)
    {
        public string Operation => $"{Method.Method} {PathTemplate}";
    }
}
