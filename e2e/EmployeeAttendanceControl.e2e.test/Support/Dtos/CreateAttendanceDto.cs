namespace EmployeeAttendanceControl.e2e.test.Support.Dtos
{
    public record CreateAttendanceDto(string FullName, DateTime Date, bool Attendance, string Notes);
}
