using Dapper;
using Moq;
using Moq.Dapper;
using ms.employees.domain.Entities;
using ms.employees.infraestucture.Data;
using ms.employees.infraestucture.Repositories;
using System.Data;
using System.Data.Common;
using Xunit;

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
