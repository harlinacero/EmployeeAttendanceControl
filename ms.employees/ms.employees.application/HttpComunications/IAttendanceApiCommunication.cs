using ms.employees.application.HttpComunications.Responses;
using Refit;

namespace ms.employees.application.HttpComunications
{
    public interface IAttendanceApiCommunication
    {
        [Get("/Attendances/GetAllAttendances")]
        Task<IEnumerable<AttendanceApiCommunicationResponse>> GetAllAttendances(string userName, [Header("Authorization")] string token);
    }
}
