using Cassandra.Mapping;

namespace ms.users.infraestructure.Data
{
    public interface IUsersContext
    {
        IMapper GetMapper();
    }
}
