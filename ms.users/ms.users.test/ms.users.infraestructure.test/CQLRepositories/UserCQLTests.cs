using ms.users.infraestructure.CQLRepositories;

namespace ms.users.infraestructure.test.CQLRepositories
{
    public class UserCQLTests
    {
        [Fact]
        public void GetUserByUserNameCql_ShouldReturnCorrectCql()
        {
            // Arrange
            var expectedCql = "SELECT * FROM user WHERE user_username = ?";
            // Act
            var actualCql = UserCQL.GetUserByUserNameCql;
            // Assert
            Assert.Equal(expectedCql, actualCql);
        }
    }
}
