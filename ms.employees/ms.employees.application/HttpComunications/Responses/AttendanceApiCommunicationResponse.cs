namespace ms.employees.application.HttpComunications.Responses
{
    public class AttendanceApiCommunicationResponse
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public DateTime Date { get; set; }
        public bool Attendance { get; set; }
        public string Notes { get; set; }
    }
}
