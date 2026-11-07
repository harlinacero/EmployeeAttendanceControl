using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

using ms.users.infraestructure.Data;
using ms.users.infraestructure.Extensions;

namespace ms.users.infraestructure.test.Data
{
    public class CassandraClusterTests
    {
        [Fact]
        public void CassandraCluster_ShouldCreateConfiguredCluster()
        {
            // Arrange
            IOptions<DatabaseSettings> options = Options.Create(new DatabaseSettings
            {
                Hostname = "localhost",
                Port = 9042,
                Keyspace = "test_keyspace"
            });
            // Act
            var cassandraCluster = new CassandraCluster(options);
            // Assert
            Assert.NotNull(cassandraCluster.ConfiguredCluster);
        }
    }
}
