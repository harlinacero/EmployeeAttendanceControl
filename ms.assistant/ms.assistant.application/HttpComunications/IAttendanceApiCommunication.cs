using ms.assistant.application.HttpComunications.Responses;
using Refit;

namespace ms.assistant.application.HttpComunications
{
    public interface IAttendanceApiCommunication
    {
        [Get("/Attendances")]
        Task<IEnumerable<AttendanceApiResponse>> GetAllAttendances(string userName, [Header("Authorization")] string token);

        [Get("/Attendances/ByDateRange")]
        Task<IEnumerable<AttendanceApiResponse>> GetAttendancesByDateRange(string userName, string from, string to, [Header("Authorization")] string token);
    }
}
