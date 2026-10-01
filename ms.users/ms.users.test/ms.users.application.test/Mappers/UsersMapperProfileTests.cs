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
            MapperConfigurationExpression expression = new();
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
                Password = "password",
                Role = "user"
            };

            // Act
            var userResponse = _mapper.Map<UserResponse>(user);
            // Assert
            Assert.Equal(user.UserName, userResponse.UserName);
            Assert.Equal(user.Password, userResponse.Password);
            Assert.Equal(user.Role, userResponse.Role);
        }

        [Fact]
        public void UsersMapperProfileShouldMapUserResponseToUser()
        {
            // Arrange
            var userResponse = new UserResponse
            {
                UserName = "testuser",
                Password = "password",
                Role = "user"
            };
            // Act
            var user = _mapper.Map<User>(userResponse);
            // Assert
            Assert.Equal(userResponse.UserName, user.UserName);
            Assert.Equal(userResponse.Password, user.Password);
            Assert.Equal(userResponse.Role, user.Role);
        }
    }
}
