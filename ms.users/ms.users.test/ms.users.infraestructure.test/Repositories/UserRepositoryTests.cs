using Cassandra;
using Cassandra.Mapping;
using Moq;
using ms.users.domain.Entities;
using ms.users.domain.Exceptions;
using ms.users.infraestructure.Data;
using ms.users.infraestructure.Repositories;
using System.Net;

namespace ms.users.infraestructure.test.Repositories
{
    public class UserRepositoryTests
    {
        private readonly Mock<IMapper> _usserMapperMock;
        private readonly Mock<IUsersContext> _usersContexMock;

        public UserRepositoryTests()
        {
            _usserMapperMock = new Mock<IMapper>();
            _usersContexMock = new Mock<IUsersContext>();
            _usersContexMock.Setup(c => c.GetMapper()).Returns(_usserMapperMock.Object);
        }

        private UserRepository CreateRepository() => new(_usersContexMock.Object);

        [Fact]
        public async Task CreateUserShouldReturnUserWhenUserIsCreated()
        {
            // Arrange
            var user = new User { UserName = "testuser", PasswordHash = "hash", Role = "admin" };
            _usserMapperMock.Setup(m => m.InsertIfNotExistsAsync<User>(user))
                .ReturnsAsync(new AppliedInfo<User>(true));

            // Act
            var result = await CreateRepository().CreateUser(user);

            // Assert
            Assert.Equal(user, result);
        }

        [Fact]
        public async Task CreateUserShouldThrowUserAlreadyExistsWhenNotApplied()
        {
            // Arrange
            var user = new User { UserName = "testuser", PasswordHash = "hash", Role = "admin" };
            _usserMapperMock.Setup(m => m.InsertIfNotExistsAsync<User>(user))
                .ReturnsAsync(new AppliedInfo<User>(false));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserAlreadyExistsException>(() => CreateRepository().CreateUser(user));
            Assert.Contains("testuser", exception.Message);
        }

        [Fact]
        public async Task GetAllUsersShouldReturnListOfUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new() { UserName = "user1", PasswordHash = "hash1", Role = "admin" },
                new() { UserName = "user2", PasswordHash = "hash2", Role = "user" }
            };
            _usserMapperMock.Setup(m => m.Fetch<User>())
                .Returns(users);

            // Act
            var result = await CreateRepository().GetAllUsers();

            // Assert
            Assert.Equal(users, result);
        }

        [Fact]
        public async Task GetUserByUserNameShouldQueryLowercasedUserName()
        {
            // Arrange
            var user = new User { UserName = "testuser", PasswordHash = "hash", Role = "admin" };
            _usserMapperMock.Setup(m => m.FirstOrDefaultAsync<User>(It.IsAny<string>(), It.Is<object[]>(args => (string)args[0] == "testuser")))
                .ReturnsAsync(user);

            // Act
            var result = await CreateRepository().GetUserByUserName("TestUser");

            // Assert
            Assert.Equal(user, result);
        }

        [Fact]
        public async Task GetUserByUserNameShouldReturnNullWhenUserDoesNotExist()
        {
            // Arrange
            _usserMapperMock.Setup(m => m.FirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync((User)null!);

            // Act
            var result = await CreateRepository().GetUserByUserName("nobody");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByUserNameShouldThrowDataUnavailableWhenDriverFails()
        {
            // Arrange
            _usserMapperMock.Setup(m => m.FirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ThrowsAsync(new NoHostAvailableException(new Dictionary<IPEndPoint, Exception>()));

            // Act & Assert
            await Assert.ThrowsAsync<DataUnavailableException>(() => CreateRepository().GetUserByUserName("testuser"));
        }

        [Fact]
        public async Task GetUserByUserNameShouldThrowDataUnavailableWhenConnectionFails()
        {
            // Arrange
            _usersContexMock.Setup(c => c.GetMapper()).Throws(new NoHostAvailableException(new Dictionary<IPEndPoint, Exception>()));

            // Act & Assert
            await Assert.ThrowsAsync<DataUnavailableException>(() => CreateRepository().GetUserByUserName("testuser"));
        }

        [Fact]
        public async Task ProtectPasswordShouldUpdateOnlyIfValueIsUnchanged()
        {
            // Arrange
            Cql? executed = null;
            _usserMapperMock.Setup(m => m.UpdateIfAsync<User>(It.IsAny<Cql>()))
                .Callback((Cql cql) => executed = cql)
                .ReturnsAsync(new AppliedInfo<User>(true));

            // Act
            var applied = await CreateRepository().ProtectPassword("admin", "1234", "pbkdf2-sha256$new");

            // Assert
            Assert.True(applied);
            Assert.NotNull(executed);
            Assert.Equal("SET user_pasword = ? WHERE user_username = ? IF user_pasword = ?", executed.Statement);
            Assert.Equal(["pbkdf2-sha256$new", "admin", "1234"], executed.Arguments);
        }
    }
}
