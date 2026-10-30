namespace ms.assistant.application.HttpComunications.Responses
{
    public class EmployeeApiResponse
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime LastAttendance { get; set; }
        public bool? LastAttendanceState { get; set; }
        public string LastAttendanceNotes { get; set; }
    }
}
