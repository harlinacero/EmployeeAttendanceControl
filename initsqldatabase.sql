CREATE DATABASE EmployeesAttendance
GO
USE EmployeesAttendance
GO
CREATE TABLE dbo.Employee 
(
	empl_username varchar(50) PRIMARY KEY NOT NULL,
	empl_firstname varchar(100) NOT NULL,
	empl_lastname varchar(100) NOT NULL,
	empl_lastattendance DATETIME2 NULL,
	empl_state int default(0),
	empl_notes varchar(max) NULL
)
GO
INSERT INTO dbo.Employee (empl_username,
	empl_firstname,
	empl_lastname,
	empl_lastattendance,
	empl_state,
	empl_notes)
VALUES ('rserrano', 'Ramon', 'Serrano Valero', NULL, 0, NULL);