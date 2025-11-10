-- ============================================
-- Employee Project Management Database
-- Microsoft SQL Server
-- ============================================

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Resource_Allocations')
BEGIN
    CREATE DATABASE Resource_Allocations;
    PRINT 'Database Resource_Allocations created successfully.';
END
ELSE
BEGIN
    PRINT 'Database Resource_Allocations already exists.';
END
GO

-- Use the Resource_Allocations database
USE Resource_Allocations;
GO

-- Drop tables if they exist (for clean execution)
IF OBJECT_ID('dbo.EmployeeProjects', 'U') IS NOT NULL
    DROP TABLE dbo.EmployeeProjects;
IF OBJECT_ID('dbo.Projects', 'U') IS NOT NULL
    DROP TABLE dbo.Projects;
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL
    DROP TABLE dbo.Employees;
GO

-- ============================================
-- Table 1: Employees
-- ============================================
CREATE TABLE Employees (
    EmployeeId INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Status NVARCHAR(10) NOT NULL,
    
    -- Constraints
    CONSTRAINT UQ_Employees_Email UNIQUE (Email),
    CONSTRAINT CK_Employees_Status CHECK (Status IN ('Active', 'Inactive'))
);
GO

-- ============================================
-- Table 2: Projects
-- ============================================
CREATE TABLE Projects (
    ProjectId INT PRIMARY KEY IDENTITY(1,1),
    ProjectName NVARCHAR(200) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    
    -- Constraints
    CONSTRAINT CK_Projects_DateRange CHECK (EndDate >= StartDate)
);
GO

-- ============================================
-- Table 3: EmployeeProjects
-- ============================================
CREATE TABLE EmployeeProjects (
    EmployeeProjectId INT PRIMARY KEY IDENTITY(1,1),
    EmployeeId INT NOT NULL,
    ProjectId INT NOT NULL,
    AssignedOn DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    
    -- Foreign Key Constraints
    CONSTRAINT FK_EmployeeProjects_Employees 
        FOREIGN KEY (EmployeeId) 
        REFERENCES Employees(EmployeeId)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    
    CONSTRAINT FK_EmployeeProjects_Projects 
        FOREIGN KEY (ProjectId) 
        REFERENCES Projects(ProjectId)
        ON DELETE CASCADE
        ON UPDATE CASCADE,
    
    -- Unique Constraint (prevents duplicate assignments)
    CONSTRAINT UQ_EmployeeProjects_Employee_Project 
        UNIQUE (EmployeeId, ProjectId)
);
GO

-- ============================================
-- Sample Data for Testing
-- ============================================

-- Insert sample employees
INSERT INTO Employees (FullName, Email, Status) VALUES
('John Doe', 'john.doe@company.com', 'Active'),
('Jane Smith', 'jane.smith@company.com', 'Active'),
('Bob Johnson', 'bob.johnson@company.com', 'Inactive'),
('Alice Williams', 'alice.williams@company.com', 'Active');
GO

-- Insert sample projects
INSERT INTO Projects (ProjectName, StartDate, EndDate) VALUES
('Website Redesign', '2024-01-01', '2024-06-30'),
('Mobile App Development', '2024-03-15', '2024-12-31'),
('Database Migration', '2024-02-01', '2024-04-30'),
('AI Integration', '2024-05-01', '2025-03-31');
GO

-- Insert sample employee-project assignments
INSERT INTO EmployeeProjects (EmployeeId, ProjectId) VALUES
(1, 1),  -- John Doe on Website Redesign
(1, 2),  -- John Doe on Mobile App
(2, 1),  -- Jane Smith on Website Redesign
(2, 3),  -- Jane Smith on Database Migration
(4, 2),  -- Alice Williams on Mobile App
(4, 4);  -- Alice Williams on AI Integration
GO

-- ============================================
-- Verification Queries
-- ============================================

-- View all tables and their constraints
SELECT 
    t.name AS TableName,
    c.name AS ConstraintName,
    c.type_desc AS ConstraintType
FROM sys.tables t
INNER JOIN sys.objects c ON t.object_id = c.parent_object_id
WHERE t.name IN ('Employees', 'Projects', 'EmployeeProjects')
    AND c.type IN ('PK', 'UQ', 'F', 'C', 'D')
ORDER BY t.name, c.type_desc;
GO

-- View all data with relationships
SELECT 
    e.EmployeeId,
    e.FullName,
    e.Email,
    e.Status,
    p.ProjectName,
    p.StartDate,
    p.EndDate,
    ep.AssignedOn,
    ep.IsActive
FROM Employees e
INNER JOIN EmployeeProjects ep ON e.EmployeeId = ep.EmployeeId
INNER JOIN Projects p ON ep.ProjectId = p.ProjectId
ORDER BY e.FullName, p.ProjectName;
GO