using AutoMapper;
using Moq;
using ms.users.application.Queries;
using ms.users.application.Queries.Handlers;
using ms.users.application.Responses;
using ms.users.domain.Entities;
using ms.users.domain.Interfaces;

namespace ms.users.application.test.Queries
{
    public class GetAllUsersQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;

        public GetAllUsersQueryHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _mapperMock = new Mock<IMapper>();
        }

        [Fact]
        public async Task HandleShouldReturnMappedUserResponses()
        {
            // Arrange
            var users = new List<User>
            {
                new() { UserName = "user1", Password = "password1", Role = "role1" },
                new() { UserName = "user2", Password = "password2", Role = "role2" }
            };
            var userResponses = new List<UserResponse>
            {
                new() { UserName = "user1", Password = "password1", Role = "role1" },
                new() { UserName = "user2", Password = "password2", Role = "role2" }
            };

            _userRepositoryMock.Setup(repo => repo.GetAllUsers())
                .ReturnsAsync(users);
            _mapperMock.Setup(mapper => mapper.Map<IEnumerable<UserResponse>>(users))
                .Returns(userResponses);
            var handler = new GetAllUsersQueryHandler(_userRepositoryMock.Object, _mapperMock.Object);

            // Act
            var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(userResponses, result);
        }
    }
}
