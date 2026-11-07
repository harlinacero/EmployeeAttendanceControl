using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ms.users.api.Controllers;
using ms.users.application.Commands;
using ms.users.application.Queries;
using ms.users.application.Request;
using ms.users.application.Responses;
using ms.users.domain.Exceptions;

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
            var result = await controller.Users();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
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
            var result = await controller.Account(accountRequest);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userResponse = Assert.IsType<string>(okResult.Value);
            Assert.Equal(userName, userResponse);
        }

        [Fact]
        public async Task AccountShouldReturnConflictWhenUserExists()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserAccountCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UserAlreadyExistsException("existing"));
            var controller = new UserController(_mediatorMock.Object);

            // Act
            var result = await controller.Account(new AccountRequest { UserName = "existing", Password = "password", Role = "user" });

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }
    }
}
