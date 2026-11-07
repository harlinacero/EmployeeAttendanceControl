using ms.users.domain.Entities;

namespace ms.users.domain.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllUsers();
        Task<User> GetUserByUserName(string userName);
        Task<User> CreateUser(User user);
        Task<bool> ProtectPassword(string userName, string currentValue, string protectedValue);
    }
}
