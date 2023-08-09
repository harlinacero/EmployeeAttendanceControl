using Cassandra.Mapping;
using Microsoft.Extensions.Configuration;
using ms.users.infraestructure.Mappings;

namespace ms.users.infraestructure.Data
{
    public class UsersContext : IUsersContext
    {
        private readonly IMapper _usersMappers;
        private readonly CassandraUserMapping _cassandraUserMapping;

        public UsersContext(CassandraCluster cassandraCluster, CassandraUserMapping cassandraUserMapping, IConfiguration configuration)
        {
            var keyspace = configuration.GetSection("DatabaseSettings:Keyspace").Value;
            var session = cassandraCluster.ConfiguredCluster.Connect(keyspace);
            
            // Create connection instance 
            _usersMappers = new Mapper(session);
            _cassandraUserMapping = cassandraUserMapping;
        }
        public IMapper GetMapper() => _usersMappers;
    }
}
