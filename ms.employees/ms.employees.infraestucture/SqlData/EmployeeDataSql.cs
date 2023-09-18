namespace ms.employees.infraestucture.SqlData
{
    public class EmployeeDataSql
    {
        public const string GetAll = @"SELECT empl_username UserName, 
                                            empl_firstName FirstName,
                                            empl_lastname LastName,
                                            empl_lastattendance LastAttendance,
                                            empl_state LastAttendanceState,
                                            empl_notes LastAttendanceNotes
                                       FROM dbo.Employee
                                       ORDER BY empl_username";

        public const string Create = @"INSERT INTO dbo.Employee (empl_username, 
                                            empl_firstName,
                                            empl_lastname,
                                            empl_lastattendance,
                                            empl_state,
                                            empl_notes)
                                        VALUES (@UserName, @FirstName, @LastName, GetDate(), @State, @Notes)";

        public const string Update = @"UPDATE dbo.Employee 
                                       SET empl_lastattendance = GetDate(),
                                            empl_state = @State,
                                            empl_notes = @Notes
                                        WHERE empl_username = @UserName";

        public const string GetEmployee = @"SELECT empl_username UserName, 
                                            empl_firstName FirstName,
                                            empl_lastname LastName,
                                            empl_lastattendance LastAttendanceDate,
                                            empl_state LastAttendanceState,
                                            empl_notes LastAttendanceNotes
                                       FROM dbo.Employee
                                       WHERE empl_username = @UserName";
    }
}
