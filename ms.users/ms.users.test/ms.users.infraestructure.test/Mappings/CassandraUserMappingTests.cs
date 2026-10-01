using ms.users.domain.Entities;
using ms.users.infraestructure.Mappings;

namespace ms.users.infraestructure.test.Mappings
{
    public class CassandraUserMappingTests
    {
        [Fact]
        public void CassandraUserMapping_ShouldDefineMappingForUserEntity()
        {
            // Arrange
            var mapping = new CassandraUserMapping();

            // Act
            var userMapping = mapping.For<User>();

            // Assert
            Assert.NotNull(userMapping);
            var tableName = userMapping.TableName("table");
            var columns = tableName.ExplicitColumns();
            Assert.NotNull(columns);
        }
    }
}
