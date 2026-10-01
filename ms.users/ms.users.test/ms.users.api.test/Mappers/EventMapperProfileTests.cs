using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ms.users.api.Events;
using ms.users.api.Mappers;
using ms.users.application.Commands;

namespace ms.users.api.test.Mappers
{
    public class EventMapperProfileTests
    {
        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public EventMapperProfileTests()
        {
            MapperConfigurationExpression expression = new();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EventMapperProfile>();
            }, _loggerFactoryMock.Object);

            _mapper = _configuration.CreateMapper();
        }

        [Fact]
        public void ShouldMapCreateUserAccountCommandToEmployeeCreateEvent()
        {
            // Arrange
            CreateUserAccountCommand command = new("testuser", "testpassword", "Admin");
            
            // Act
            var result =_mapper.Map<EmployeeCreateEvent>(command);

            // Assert
            Assert.Equal(command.UserName, result.UserName);
            Assert.Equal(command.Password, result.Password);
            Assert.Equal(command.Role, result.Role);
        }

        [Fact]
        public void ShouldMapEmployeeCreateEventToCreateUserAccountCommand()
        {
            // Arrange
            EmployeeCreateEvent evento = new() { UserName = "testuser", Password = "testpassword", Role = "Admin" };

            // Act
            var command = _mapper.Map<CreateUserAccountCommand>(evento);

            // Assert
            Assert.Equal(command.UserName, evento.UserName);
            Assert.Equal(command.Password, evento.Password);
            Assert.Equal(command.Role, evento.Role);
        }
    }
}
