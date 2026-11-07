using ms.employees.application.HttpComunications.Requests;
using Refit;

namespace ms.employees.application.HttpComunications
{
    public interface IUsersApiCommunication
    {
        [Post("/User")]
        Task CreateAccount([Body] CreateAccountApiRequest account, [Header("Authorization")] string token);
    }
}
