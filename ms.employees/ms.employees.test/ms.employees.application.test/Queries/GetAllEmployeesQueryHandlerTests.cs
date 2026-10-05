using AutoMapper;
using Moq;
using ms.employees.application.Queries;
using ms.employees.application.Queries.Handlers;
using ms.employees.application.Responses;
using ms.employees.domain.Entities;
using ms.employees.domain.Repositories;

namespace ms.employees.application.test.Queries
{
    public class GetAllEmployeesQueryHandlerTests
    {
        private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;

        public GetAllEmployeesQueryHandlerTests()
        {
            _employeeRepositoryMock = new Mock<IEmployeeRepository>();
            _mapperMock = new Mock<IMapper>();
        }

        [Fact]
        public async Task Handle_ShouldReturnMappedEmployeeResponses()
        {
            // Arrange
            var employees = new List<Employee>
            {
                new() { UserName = "user1", FirstName = "John", LastName = "Doe" },
                new() { UserName = "user2", FirstName = "Jane", LastName = "Smith" }
            };
            var employeeResponses = new List<EmployeeResponse>
            {
                new() { UserName = "user1", FirstName = "John", LastName = "Doe" },
                new() { UserName = "user2", FirstName = "Jane", LastName = "Smith" }
            };
            _employeeRepositoryMock.Setup(repo => repo.GetEmployeesAsync())
                .ReturnsAsync(employees);
            _mapperMock.Setup(mapper => mapper.Map<IEnumerable<ms.employees.application.Responses.EmployeeResponse>>(employees))
                .Returns(employeeResponses);
            var handler = new GetAllEmployeesQueryHandler(_employeeRepositoryMock.Object, _mapperMock.Object);
            var query = new GetAllEmployeesQuery();
            // Act
            var result = await handler.Handle(query, CancellationToken.None);
            // Assert
            Assert.Equal(employeeResponses, result);
        }
    }
}
