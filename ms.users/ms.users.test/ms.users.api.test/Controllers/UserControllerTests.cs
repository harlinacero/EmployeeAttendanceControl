using MediatR;
using Moq;
using ms.users.api.Controllers;
using ms.users.application.Commands;
using ms.users.application.Queries;
using ms.users.application.Request;
using ms.users.application.Responses;

namespace ms.users.api.test.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        public UserControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public async Task SendGetAllUsersQueryWhenGetAllUsersIsCalled()
        {
            // Arrange
            var expectedUsers = new List<UserResponse> { new UserResponse { UserName = "user1" }, new UserResponse { UserName = "user2" } };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUsers);

            var controller = new UserController(_mediatorMock.Object);

            //act
            var result = await controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
            var users = Assert.IsType<List<UserResponse>>(okResult.Value);
            Assert.Equal(expectedUsers, users);
        }

        [Fact]
        public async Task SendCreateUserAccountCommandWhenCreateAccountIsCalled()
        {
            // Arrange
            var userName = "newuser";
            var accountRequest = new AccountRequest { UserName = userName, Password = "password", Role = "user" };

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(userName);
            var controller = new UserController(_mediatorMock.Object);
            //act
            var result = await controller.CreateAccount(accountRequest);
            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
            var userResponse = Assert.IsType<string>(okResult.Value);
            Assert.Equal(userName, userResponse);
        }
    }
}
