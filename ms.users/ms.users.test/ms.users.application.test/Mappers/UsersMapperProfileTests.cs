using AutoMapper;
using Moq;
using Microsoft.Extensions.Logging;
using ms.users.application.Responses;
using ms.users.application.Mappers;
using ms.users.domain.Entities;

namespace ms.users.application.test.Mappers
{
    public class UsersMapperProfileTests
    {
        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public UsersMapperProfileTests()
        {
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<UsersMapperProfile>();
            }, _loggerFactoryMock.Object);
            _mapper = _configuration.CreateMapper();
        }

        [Fact]
        public void UsersMapperProfileShouldHaveValidConfiguration()
        {
            // Act & Assert
            _configuration.AssertConfigurationIsValid();
        }

        [Fact]
        public void UsersMapperProfileShouldMapUserToUserResponse()
        {
            // Arrange
            var user = new User
            {
                UserName = "testuser",
                PasswordHash = "pbkdf2-sha256$hashed",
                Role = "user"
            };

            // Act
            var userResponse = _mapper.Map<UserResponse>(user);

            // Assert
            Assert.Equal(user.UserName, userResponse.UserName);
            Assert.Equal(user.Role, userResponse.Role);
        }

        [Fact]
        public void UserResponseShouldNotExposePassword()
        {
            // Act
            var properties = typeof(UserResponse).GetProperties().Select(property => property.Name);

            // Assert
            Assert.DoesNotContain(properties, name => name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        }
    }
}
