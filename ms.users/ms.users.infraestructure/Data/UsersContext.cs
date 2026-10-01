using Cassandra.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ms.users.infraestructure.Extensions;
using ms.users.infraestructure.Mappings;
using System.Diagnostics.CodeAnalysis;

namespace ms.users.infraestructure.Data
{
    [ExcludeFromCodeCoverage]
    public class UsersContext : IUsersContext
    {
        private readonly IMapper _usersMappers;
        private readonly CassandraUserMapping _cassandraUserMapping;

        public UsersContext(CassandraCluster cassandraCluster, CassandraUserMapping cassandraUserMapping, IOptions<DatabaseSettings> configuration)
        {
            var keyspace = configuration.Value.Keyspace;
            var session = cassandraCluster.ConfiguredCluster.Connect(keyspace);
            
            // Create connection instance 
            _usersMappers = new Mapper(session);
            _cassandraUserMapping = cassandraUserMapping;
        }
        public IMapper GetMapper() => _usersMappers;
    }
}
