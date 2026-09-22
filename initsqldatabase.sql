IF DB_ID(N'EmployeesAttendance') IS NULL
BEGIN
    CREATE DATABASE EmployeesAttendance
END
GO
USE EmployeesAttendance
GO
IF OBJECT_ID(N'dbo.Employee', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Employee 
(
	empl_username varchar(50) PRIMARY KEY NOT NULL,
	empl_firstname varchar(100) NOT NULL,
	empl_lastname varchar(100) NOT NULL,
	empl_lastattendance DATETIME2 NULL,
	empl_state int default(0),
	empl_notes varchar(max) NULL
)
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Employee WHERE empl_username = 'hacero')
BEGIN
INSERT INTO dbo.Employee (empl_username,
	empl_firstname,
	empl_lastname,
	empl_lastattendance,
	empl_state,
	empl_notes)
VALUES ('hacero', 'Harlin', 'Acero', NULL, 0, NULL);
END