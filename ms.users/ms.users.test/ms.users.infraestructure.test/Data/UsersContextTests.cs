using Cassandra;
using Microsoft.Extensions.Options;
using Moq;
using ms.users.infraestructure.Data;
using ms.users.infraestructure.Extensions;
using ms.users.infraestructure.Mappings;
using System.Net;

namespace ms.users.infraestructure.test.Data
{
    public class UsersContextTests
    {
        private readonly Mock<ICluster> _clusterMock;
        private readonly Mock<ISession> _sessionMock;
        private readonly UsersContext _context;

        public UsersContextTests()
        {
            _clusterMock = new Mock<ICluster>();
            _sessionMock = new Mock<ISession>();
            // The driver's Mapper reads the session's configuration when it's built
            var sessionClusterMock = new Mock<ICluster>();
            sessionClusterMock.SetupGet(cluster => cluster.Configuration).Returns(Cluster.Builder().AddContactPoint("127.0.0.1").GetConfiguration());
            _sessionMock.SetupGet(session => session.Cluster).Returns(sessionClusterMock.Object);
            var settings = Options.Create(new DatabaseSettings { Hostname = "localhost", Port = 9042, Keyspace = "kusers" });
            var cassandraCluster = new CassandraCluster(settings) { ConfiguredCluster = _clusterMock.Object };
            _context = new UsersContext(cassandraCluster, new CassandraUserMapping(), new CassandraSessionMapping(), settings);
        }

        [Fact]
        public void GetMapperShouldConnectOnlyOnce()
        {
            // Arrange
            _clusterMock.Setup(cluster => cluster.Connect("kusers")).Returns(_sessionMock.Object);

            // Act
            var first = _context.GetMapper();
            var second = _context.GetMapper();

            // Assert
            Assert.Same(first, second);
            _clusterMock.Verify(cluster => cluster.Connect("kusers"), Times.Once);
        }

        [Fact]
        public void GetMapperShouldRetryAfterFailedConnection()
        {
            // Arrange
            _clusterMock.SetupSequence(cluster => cluster.Connect("kusers"))
                .Throws(new NoHostAvailableException(new Dictionary<IPEndPoint, Exception>()))
                .Returns(_sessionMock.Object);

            // Act
            Assert.Throws<NoHostAvailableException>(() => _context.GetMapper());
            var mapper = _context.GetMapper();

            // Assert
            Assert.NotNull(mapper);
            _clusterMock.Verify(cluster => cluster.Connect("kusers"), Times.Exactly(2));
        }

        [Fact]
        public void DisposeShouldCloseSessionAndCluster()
        {
            // Arrange
            _clusterMock.Setup(cluster => cluster.Connect("kusers")).Returns(_sessionMock.Object);
            _context.GetMapper();

            // Act
            _context.Dispose();

            // Assert
            _sessionMock.Verify(session => session.Dispose(), Times.Once);
            _clusterMock.Verify(cluster => cluster.Dispose(), Times.Once);
        }
    }
}
