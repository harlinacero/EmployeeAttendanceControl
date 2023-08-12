using ms.employees.domain.Entities;

namespace ms.employees.domain.Repositories
{
    public interface IEmployeeRepository
    {
        Task<IEnumerable<Employee>> GetEmployeesAsync();
        Task<string> UpdateAttendanceStateEmp(string userName, bool attendance, string notes);
        Task<string> CreateEmployee(Employee employee);
    }
}
