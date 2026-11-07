using Cassandra;
using Microsoft.Extensions.Options;
using ms.users.infraestructure.Extensions;

namespace ms.users.infraestructure.Data
{
    public class CassandraCluster
    {
        public ICluster ConfiguredCluster { get; set; }

        public CassandraCluster(IOptions<DatabaseSettings> configuration)
        {
            var hostname = configuration.Value.Hostname;
            var port = configuration.Value.Port;
            ConfiguredCluster = Cluster.Builder().AddContactPoint(hostname)
                                                .WithPort(port)
                                                .Build();

        }
    }
}
