namespace ms.attendances.infraestucture.Extensions
{
    public class ConnectionStrings
    {
        public static string ConnectionStringsSection = "ConnectionStrings";

        public string MongoDB { get; set; }
        public string AttendanceDB { get; set; }
        public string AttendanceCollection { get; set; }
    }
}
