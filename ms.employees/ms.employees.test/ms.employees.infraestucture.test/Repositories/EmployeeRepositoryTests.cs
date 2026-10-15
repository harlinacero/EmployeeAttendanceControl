using Moq;
using ms.employees.infraestucture.Data;
using System.Data;

namespace ms.employees.infraestucture.test.Repositories
{
    public class EmployeeRepositoryTests
    {
        private readonly Mock<IDapperContext> _mockDapperContext;
        private readonly Mock<IDbConnection> _connection;

        public EmployeeRepositoryTests()
        {
            _mockDapperContext = new Mock<IDapperContext>();
            _connection = new Mock<IDbConnection>();
        }


    }   

}
