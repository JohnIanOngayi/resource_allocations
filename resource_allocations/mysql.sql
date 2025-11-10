-- ============================================
-- Employee Project Management Database
-- MySQL
-- ============================================

-- Create database if it doesn't exist
CREATE DATABASE IF NOT EXISTS Resource_Allocations;

SELECT 'Database Resource_Allocations created or already exists.' AS Message;

-- Use the Resource_Allocations database
USE Resource_Allocations;

-- Drop tables if they exist (for clean execution)
DROP TABLE IF EXISTS EmployeeProjects;
DROP TABLE IF EXISTS Projects;
DROP TABLE IF EXISTS Employees;

-- ============================================
-- Table 1: Employees
-- ============================================
CREATE TABLE Employees (
    EmployeeId INT PRIMARY KEY AUTO_INCREMENT,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Status VARCHAR(10) NOT NULL,
    
    -- Constraints
    CONSTRAINT UQ_Employees_Email UNIQUE (Email),
    CONSTRAINT CK_Employees_Status CHECK (Status IN ('Active', 'Inactive'))
);

-- ============================================
-- Table 2: Projects
-- ============================================
CREATE TABLE Projects (
    ProjectId INT PRIMARY KEY AUTO_INCREMENT,
    ProjectName VARCHAR(200) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    
    -- Constraints
    CONSTRAINT CK_Projects_DateRange CHECK (EndDate >= StartDate)
);

-- ============================================
-- Table 3: EmployeeProjects
-- ============================================
CREATE TABLE EmployeeProjects (
    EmployeeProjectId INT PRIMARY KEY AUTO_INCREMENT,
    EmployeeId INT NOT NULL,
    ProjectId INT NOT NULL,
    AssignedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    
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

-- ============================================
-- Sample Data for Testing
-- ============================================

-- Insert sample employees
INSERT INTO Employees (FullName, Email, Status) VALUES
('John Doe', 'john.doe@company.com', 'Active'),
('Jane Smith', 'jane.smith@company.com', 'Active'),
('Bob Johnson', 'bob.johnson@company.com', 'Inactive'),
('Alice Williams', 'alice.williams@company.com', 'Active');

-- Insert sample projects
INSERT INTO Projects (ProjectName, StartDate, EndDate) VALUES
('Website Redesign', '2024-01-01', '2024-06-30'),
('Mobile App Development', '2024-03-15', '2024-12-31'),
('Database Migration', '2024-02-01', '2024-04-30'),
('AI Integration', '2024-05-01', '2025-03-31');

-- Insert sample employee-project assignments
INSERT INTO EmployeeProjects (EmployeeId, ProjectId) VALUES
(1, 1),  -- John Doe on Website Redesign
(1, 2),  -- John Doe on Mobile App
(2, 1),  -- Jane Smith on Website Redesign
(2, 3),  -- Jane Smith on Database Migration
(4, 2),  -- Alice Williams on Mobile App
(4, 4);  -- Alice Williams on AI Integration

-- ============================================
-- Verification Queries
-- ============================================

-- View all tables and their constraints
SELECT 
    TABLE_NAME AS TableName,
    CONSTRAINT_NAME AS ConstraintName,
    CONSTRAINT_TYPE AS ConstraintType
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE TABLE_SCHEMA = 'Resource_Allocations'
    AND TABLE_NAME IN ('Employees', 'Projects', 'EmployeeProjects')
ORDER BY TABLE_NAME, CONSTRAINT_TYPE;

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

-- ============================================
-- PART 2: STORED PROCEDURE WITH VALIDATION
-- ============================================

-- Drop procedure if it exists
DROP PROCEDURE IF EXISTS AssignEmployeeToProject;

DELIMITER //

CREATE PROCEDURE AssignEmployeeToProject(
    IN p_EmployeeId INT,
    IN p_ProjectId INT
)
BEGIN
    -- Declare variables for validation
    DECLARE v_EmployeeExists INT DEFAULT 0;
    DECLARE v_EmployeeStatus VARCHAR(10);
    DECLARE v_ProjectExists INT DEFAULT 0;
    DECLARE v_EmployeeActiveProjectCount INT DEFAULT 0;
    DECLARE v_ProjectActiveEmployeeCount INT DEFAULT 0;
    DECLARE v_AlreadyAssigned INT DEFAULT 0;
    
    -- Declare handler for SQL exceptions
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        -- Rollback transaction on error
        ROLLBACK;
        RESIGNAL;
    END;
    
    -- Start Transaction
    START TRANSACTION;
    
    -- ============================================
    -- VALIDATION 1: Check if employee exists and is Active
    -- ============================================
    SELECT 
        COUNT(*),
        IFNULL(MAX(Status), '')
    INTO 
        v_EmployeeExists,
        v_EmployeeStatus
    FROM Employees
    WHERE EmployeeId = p_EmployeeId;
    
    IF v_EmployeeExists = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Employee does not exist.';
    END IF;
    
    IF v_EmployeeStatus <> 'Active' THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Employee is not Active. Only active employees can be assigned to projects.';
    END IF;
    
    -- ============================================
    -- VALIDATION 2: Check if project exists
    -- ============================================
    SELECT COUNT(*)
    INTO v_ProjectExists
    FROM Projects
    WHERE ProjectId = p_ProjectId;
    
    IF v_ProjectExists = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Project does not exist.';
    END IF;
    
    -- ============================================
    -- VALIDATION 3: Check if employee already assigned to this project
    -- ============================================
    SELECT COUNT(*)
    INTO v_AlreadyAssigned
    FROM EmployeeProjects
    WHERE EmployeeId = p_EmployeeId 
        AND ProjectId = p_ProjectId
        AND IsActive = 1;
    
    IF v_AlreadyAssigned > 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Employee is already assigned to this project.';
    END IF;
    
    -- ============================================
    -- VALIDATION 4: Check if employee is assigned to 3 or more active projects
    -- ============================================
    SELECT COUNT(*)
    INTO v_EmployeeActiveProjectCount
    FROM EmployeeProjects
    WHERE EmployeeId = p_EmployeeId
        AND IsActive = 1;
    
    IF v_EmployeeActiveProjectCount >= 3 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Employee is already assigned to 3 or more active projects. Cannot assign to more projects.';
    END IF;
    
    -- ============================================
    -- VALIDATION 5: Check if project has 10 or more active employees
    -- ============================================
    SELECT COUNT(*)
    INTO v_ProjectActiveEmployeeCount
    FROM EmployeeProjects
    WHERE ProjectId = p_ProjectId
        AND IsActive = 1;
    
    IF v_ProjectActiveEmployeeCount >= 10 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Project already has 10 or more active employees. Cannot assign more employees.';
    END IF;
    
    -- ============================================
    -- ALL VALIDATIONS PASSED - Insert new assignment
    -- ============================================
    INSERT INTO EmployeeProjects (EmployeeId, ProjectId, IsActive)
    VALUES (p_EmployeeId, p_ProjectId, 1);
    
    -- Commit the transaction
    COMMIT;
    
    -- Return success message
    SELECT 
        'Success' AS Result,
        p_EmployeeId AS EmployeeId,
        p_ProjectId AS ProjectId,
        NOW() AS AssignedOn;
    
END //

DELIMITER ;

-- ============================================
-- TEST CASES FOR STORED PROCEDURE
-- ============================================

-- SELECT '======================================== TEST CASE 1: Valid Assignment' AS TestCase;
-- CALL AssignEmployeeToProject(3, 1);

-- SELECT '' AS Separator;
-- SELECT '======================================== TEST CASE 2: Employee Does Not Exist' AS TestCase;
-- This will throw an error
-- CALL AssignEmployeeToProject(999, 1);

-- SELECT '' AS Separator;
-- SELECT '======================================== TEST CASE 3: Employee is Inactive' AS TestCase;
-- CALL AssignEmployeeToProject(3, 2);

-- SELECT '' AS Separator;
-- SELECT '======================================== TEST CASE 4: Project Does Not Exist' AS TestCase;
-- CALL AssignEmployeeToProject(1, 999);

-- SELECT '' AS Separator;
-- SELECT '======================================== TEST CASE 5: Employee Already Assigned' AS TestCase;
-- CALL AssignEmployeeToProject(1, 1);

-- SELECT '' AS Separator;
-- SELECT '======================================== View Current Assignments' AS TestCase;
-- SELECT 
--     e.EmployeeId,
--     e.FullName,
--     e.Status,
--     p.ProjectId,
--     p.ProjectName,
--     ep.AssignedOn,
--     ep.IsActive
-- FROM Employees e
-- INNER JOIN EmployeeProjects ep ON e.EmployeeId = ep.EmployeeId
-- INNER JOIN Projects p ON ep.ProjectId = p.ProjectId
-- WHERE ep.IsActive = 1
-- ORDER BY e.FullName, p.ProjectName;

-- ============================================
-- Helper Procedure: View Employee Project Count
-- ============================================
DROP PROCEDURE IF EXISTS GetEmployeeProjectCount;

DELIMITER //

CREATE PROCEDURE GetEmployeeProjectCount(
    IN p_EmployeeId INT
)
BEGIN
    SELECT 
        e.EmployeeId,
        e.FullName,
        e.Status,
        COUNT(ep.EmployeeProjectId) AS ActiveProjectCount
    FROM Employees e
    LEFT JOIN EmployeeProjects ep ON e.EmployeeId = ep.EmployeeId AND ep.IsActive = 1
    WHERE e.EmployeeId = p_EmployeeId
    GROUP BY e.EmployeeId, e.FullName, e.Status;
END//

DELIMITER ;

-- ============================================
-- Helper Procedure: View Project Employee Count
-- ============================================
DROP PROCEDURE IF EXISTS GetProjectEmployeeCount;

DELIMITER //

CREATE PROCEDURE GetProjectEmployeeCount(
    IN p_ProjectId INT
)
BEGIN
    SELECT 
        p.ProjectId,
        p.ProjectName,
        COUNT(ep.EmployeeProjectId) AS ActiveEmployeeCount
    FROM Projects p
    LEFT JOIN EmployeeProjects ep ON p.ProjectId = ep.ProjectId AND ep.IsActive = 1
    WHERE p.ProjectId = p_ProjectId
    GROUP BY p.ProjectId, p.ProjectName;
END//

DELIMITER ;