using Cassandra.Mapping;
using ms.users.domain.Entities;
using ms.users.domain.Interfaces;
using ms.users.infraestructure.CQLRepositories;
using ms.users.infraestructure.Data;

namespace ms.users.infraestructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMapper _ussersMapper;

        /// <summary>
        /// Get client mapper for invoke Query CQL
        /// </summary>
        /// <param name="usersContext"></param>
        public UserRepository(IUsersContext usersContext) => _ussersMapper = usersContext.GetMapper();

        public async Task<User> CreateUser(User user)
        {
            var appliedInfo = await _ussersMapper.InsertIfNotExistsAsync<User>(user);

            if (!appliedInfo.Applied)
            {
                throw new Exception($"User {user.UserName} already exists in DB");
            }

            return user;
        }

        public async Task<List<User>> GetAllUsers() => await Task.FromResult(_ussersMapper.Fetch<User>().ToList());

        public async Task<User> GetUser(string username, string password)
        {
            var user = await _ussersMapper.FirstOrDefaultAsync<User>(UserCQL.GetUserByUserNameCql, username.ToLowerInvariant())
                ?? throw new Exception($"User not exists");
            if (password != user.Password)
            {
                throw new Exception("Password is not correct");
            }

            return user;
        }
    }
}
