using Cassandra;
using Cassandra.Mapping;
using ms.users.domain.Entities;
using ms.users.domain.Exceptions;
using ms.users.domain.Interfaces;
using ms.users.infraestructure.CQLRepositories;
using ms.users.infraestructure.Data;

namespace ms.users.infraestructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IUsersContext _usersContext;

        public UserRepository(IUsersContext usersContext) => _usersContext = usersContext;

        public Task<User> CreateUser(User user) => CassandraCall.Run(async mapper =>
        {
            var appliedInfo = await mapper.InsertIfNotExistsAsync(user);
            if (!appliedInfo.Applied)
            {
                throw new UserAlreadyExistsException(user.UserName);
            }

            return user;
        }, _usersContext);

        public Task<List<User>> GetAllUsers() => CassandraCall.Run(mapper => Task.FromResult(mapper.Fetch<User>().ToList()), _usersContext);

        public Task<User> GetUserByUserName(string userName) =>
            CassandraCall.Run(mapper => mapper.FirstOrDefaultAsync<User>(UserCQL.GetUserByUserNameCql, userName.ToLowerInvariant()), _usersContext);

        public Task<bool> ProtectPassword(string userName, string currentValue, string protectedValue) => CassandraCall.Run(async mapper =>
        {
            var appliedInfo = await mapper.UpdateIfAsync<User>(Cql.New(UserCQL.ProtectPasswordCql, protectedValue, userName, currentValue));
            return appliedInfo.Applied;
        }, _usersContext);
    }
}
