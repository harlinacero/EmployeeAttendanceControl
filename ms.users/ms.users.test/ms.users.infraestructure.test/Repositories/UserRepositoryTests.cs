using Cassandra.Mapping;
using Moq;
using ms.users.domain.Entities;
using ms.users.infraestructure.Data;
using ms.users.infraestructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

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
        }

        [Fact]
        public async Task CreateUserShouldReturnUserWhenUserIsCreated()
        {
            // Arrange
            var user = new User
            {
                UserName = "testuser",
                Password = "password",
                Role = "admin"
            };
            AppliedInfo<User> appliedInfo = new AppliedInfo<User>(true);
            _usserMapperMock.Setup(m => m.InsertIfNotExistsAsync<User>(user))
                .ReturnsAsync(appliedInfo);
            _usersContexMock.Setup(c => c.GetMapper())
                .Returns(_usserMapperMock.Object);


            var userRepository = new UserRepository(_usersContexMock.Object);
            
            // Act
            var result = await userRepository.CreateUser(user);
            
            // Assert
            Assert.Equal(user, result);
        }

        [Fact]
        public async Task CreateUserShouldThrowExceptionWhenUserAlreadyExists()
        {
            // Arrange
            var user = new User
            {
                UserName = "testuser",
                Password = "password",
                Role = "admin"
            };
            AppliedInfo<User> appliedInfo = new AppliedInfo<User>(false);
            _usserMapperMock.Setup(m => m.InsertIfNotExistsAsync<User>(user))
                .ReturnsAsync(appliedInfo);
            _usersContexMock.Setup(c => c.GetMapper())
                .Returns(_usserMapperMock.Object);
            var userRepository = new UserRepository(_usersContexMock.Object);

            // Act & Assert
            var response = await Assert.ThrowsAsync<Exception>(() => userRepository.CreateUser(user));
            Assert.Equivalent($"User {user.UserName} already exists in DB", response.Message);
        }

        [Fact]
        public async Task GetAllUsersShouldReturnListOfUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new() { UserName = "user1", Password = "password1", Role = "admin" },
                new() { UserName = "user2", Password = "password2", Role = "user" }
            };
            _usserMapperMock.Setup(m => m.Fetch<User>())
                .Returns(users);
            _usersContexMock.Setup(c => c.GetMapper())
                .Returns(_usserMapperMock.Object);
            var userRepository = new UserRepository(_usersContexMock.Object);
            // Act
            var result = await userRepository.GetAllUsers();
            // Assert
            Assert.Equal(users, result);
        }

        [Fact]
        public async Task GetUserShouldReturnUserWhenUserExistsAndPasswordIsCorrect()
        {
            // Arrange
            var username = "testuser";
            var password = "password";
            var user = new User
            {
                UserName = username,
                Password = password,
                Role = "admin"
            };
            _usserMapperMock.Setup(m => m.FirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(user);
            _usersContexMock.Setup(c => c.GetMapper())
                .Returns(_usserMapperMock.Object);
            var userRepository = new UserRepository(_usersContexMock.Object);
            // Act
            var result = await userRepository.GetUser(username, password);
            // Assert
            Assert.Equal(user, result);
        }

        [Fact]
        public async Task GetUserShouldThrowExceptionWhenUserDoesNotExist()
        {
            // Arrange
            var username = "nonexistentuser";
            var password = "password";
            var errorMessage = "User not exists";

            _usserMapperMock.Setup(m => m.FirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ThrowsAsync(new Exception(errorMessage));
            _usersContexMock.Setup(c => c.GetMapper())
                .Returns(_usserMapperMock.Object);
            var userRepository = new UserRepository(_usersContexMock.Object);

            // Act & Assert
            var response = await Assert.ThrowsAsync<Exception>(() => userRepository.GetUser(username, password));
            Assert.Equal(errorMessage, response.Message);
        }

        [Fact]
        public async Task GetUserShouldThrowExceptionWhenPasswordIsIncorrect()
        {
            // Arrange
            var username = "testuser";
            var correctPassword = "correctpassword";
            var incorrectPassword = "incorrectpassword";
            var user = new User
            {
                UserName = username,
                Password = correctPassword,
                Role = "admin"
            };
            _usserMapperMock.Setup(m => m.FirstOrDefaultAsync<User>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(user);
            _usersContexMock.Setup(c => c.GetMapper())
                .Returns(_usserMapperMock.Object);
            var userRepository = new UserRepository(_usersContexMock.Object);
            // Act & Assert
            var response = await Assert.ThrowsAsync<Exception>(() => userRepository.GetUser(username, incorrectPassword));
            Assert.Equal("Password is not correct", response.Message);
        }
    }
}
