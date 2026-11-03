namespace EmployeeAttendanceControl.e2e.test.Support
{
    /// <summary>
    /// An employee created by this run, already able to sign in.
    /// </summary>
    public record TestEmployee(string UserName, string FirstName, string LastName, string Token)
    {
        public string FullName => $"{FirstName} {LastName}";
    }
}
