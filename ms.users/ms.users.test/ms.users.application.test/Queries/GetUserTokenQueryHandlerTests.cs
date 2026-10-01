using Microsoft.Extensions.Options;
using Moq;
using ms.users.application.Extensions;
using ms.users.application.Queries;
using ms.users.application.Queries.Handlers;
using ms.users.application.Responses;
using ms.users.domain.Entities;
using ms.users.domain.Interfaces;

namespace ms.users.application.test.Queries
{
    public class GetUserTokenQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly IOptions<SettingsOptions> _configuration;

        public GetUserTokenQueryHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();

            _configuration = Options.Create(new SettingsOptions
            {
                Authentication = new Authentication
                {
                    JWT = new JWT
                    {
                        Key = "adsfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfaasdfasdf",
                        ExpirationHours = 4
                    }
                }
            });
        }
        [Fact]
        public async Task HandleShouldReturnMappedUserResponse()
        {
            // Arrange
            var user = new User { UserName = "user1", Password = "password1", Role = "role1" };
            var userResponse = new UserResponse { UserName = "user1", Password = "password1", Role = "role1" };
            
            _userRepositoryMock.Setup(repo => repo.GetUser(user.UserName, user.Password))
                .ReturnsAsync(user);
            
            var handler = new GetUserTokenQueryHandler(_userRepositoryMock.Object, _configuration);
            
            // Act
            var request = new GetUserTokenQuery(user.UserName, user.Password);
            var result = await handler.Handle(request, CancellationToken.None);
           
            // Assert
            Assert.NotNull(result);
            Assert.IsType<string>(result);
        }

        [Fact]
        public async Task HandleShouldReturnNullForInvalidUser()
        {
            // Arrange
            var errorString = "Invalid user credentials";
            _userRepositoryMock.Setup(repo => repo.GetUser(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception(errorString));

            var handler = new GetUserTokenQueryHandler(_userRepositoryMock.Object, _configuration);

            // Act
            var request = new GetUserTokenQuery("invalidUser", "invalidPassword");
            var result = Assert.ThrowsAsync<Exception>(async () => await handler.Handle(request, CancellationToken.None));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(errorString, result.Result.Message);
        }
    }
}
