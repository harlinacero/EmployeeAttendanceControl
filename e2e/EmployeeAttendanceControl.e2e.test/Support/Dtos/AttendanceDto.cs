namespace EmployeeAttendanceControl.e2e.test.Support.Dtos
{
    public record AttendanceDto(string Id, string UserName, string FullName, DateTime Date, bool Attendance, string Notes);
}
