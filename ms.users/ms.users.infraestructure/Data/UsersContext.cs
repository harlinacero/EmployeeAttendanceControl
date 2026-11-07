using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Options;
using ms.users.infraestructure.Extensions;
using ms.users.infraestructure.Mappings;

namespace ms.users.infraestructure.Data
{
    /// <summary>
    /// One Cassandra session for the whole service (registered as a singleton). It connects on first use, and a failed
    /// connection isn't remembered: the next call tries again, so the service recovers after Cassandra comes back.
    /// </summary>
    public class UsersContext : IUsersContext, IDisposable
    {
        private readonly ICluster _cluster;
        private readonly string _keyspace;
        private readonly MappingConfiguration _mappings;
        private readonly Lock _connectionLock = new();
        private ISession _session;
        private volatile IMapper _usersMapper;

        public UsersContext(CassandraCluster cassandraCluster, CassandraUserMapping cassandraUserMapping,
            CassandraSessionMapping cassandraSessionMapping, IOptions<DatabaseSettings> configuration)
        {
            _cluster = cassandraCluster.ConfiguredCluster;
            _keyspace = configuration.Value.Keyspace;
            _mappings = new MappingConfiguration().Define(cassandraUserMapping, cassandraSessionMapping);
        }

        public IMapper GetMapper()
        {
            if (_usersMapper != null)
            {
                return _usersMapper;
            }

            lock (_connectionLock)
            {
                if (_usersMapper == null)
                {
                    _session = _cluster.Connect(_keyspace);
                    _usersMapper = new Mapper(_session, _mappings);
                }

                return _usersMapper;
            }
        }

        public void Dispose()
        {
            _session?.Dispose();
            _cluster.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
