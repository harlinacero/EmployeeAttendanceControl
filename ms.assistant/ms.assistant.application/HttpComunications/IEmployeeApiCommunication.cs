using ms.assistant.application.HttpComunications.Responses;
using Refit;

namespace ms.assistant.application.HttpComunications
{
    public interface IEmployeeApiCommunication
    {
        [Get("/Employees")]
        Task<IEnumerable<EmployeeApiResponse>> GetAllEmployees([Header("Authorization")] string token);
    }
}
