using Dapper;
using ms.employees.domain.Entities;
using ms.employees.domain.Repositories;
using ms.employees.infraestucture.Data;
using ms.employees.infraestucture.SqlData;

namespace ms.employees.infraestucture.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private IDapperContext _context;
        public EmployeeRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<string> CreateEmployee(Employee employee)
        {
            var res = await _context.Connection.ExecuteAsync(EmployeeDataSql.Create, param: new
            {
                UserName = employee.UserName,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                State = employee.LastAttendanceState,
                Notes = employee.LastAttendanceNotes
            });

            return res > 0 ? employee.UserName : throw new Exception("Usuario no creado");
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync()
        {
            var res = await _context.Connection.QueryAsync<Employee>(EmployeeDataSql.GetAll);
            return res?.ToList() ?? new List<Employee>();
        }

        public async Task<string> UpdateAttendanceStateEmp(string userName, bool attendance, string notes)
        {
            var res = await _context.Connection.ExecuteAsync(EmployeeDataSql.Update, param: new
            {
                UserName = userName,
                State = attendance,
                Notes = notes
            });

            return res > 0 ? userName : null;
        }
    }
}
