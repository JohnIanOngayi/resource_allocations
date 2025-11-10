IF OBJECT_ID('dbo.AssignEmployeeToProject', 'P') IS NOT NULL
    DROP PROCEDURE dbo.AssignEmployeeToProject;
GO

CREATE PROCEDURE AssignEmployeeToProject
    @EmployeeId INT,
    @ProjectId INT
AS
BEGIN
    -- Set NOCOUNT ON to prevent extra result sets
    SET NOCOUNT ON;
    
    -- Declare variables for validation
    DECLARE @EmployeeExists BIT = 0;
    DECLARE @EmployeeStatus NVARCHAR(10);
    DECLARE @ProjectExists BIT = 0;
    DECLARE @EmployeeActiveProjectCount INT = 0;
    DECLARE @ProjectActiveEmployeeCount INT = 0;
    DECLARE @AlreadyAssigned BIT = 0;
    
    -- Begin Transaction
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- ============================================
        -- VALIDATION 1: Check if employee exists and is Active
        -- ============================================
        SELECT 
            @EmployeeExists = 1,
            @EmployeeStatus = Status
        FROM Employees
        WHERE EmployeeId = @EmployeeId;
        
        IF @EmployeeExists = 0
        BEGIN
            THROW 50001, 'Employee does not exist.', 1;
        END
        
        IF @EmployeeStatus <> 'Active'
        BEGIN
            THROW 50002, 'Employee is not Active. Only active employees can be assigned to projects.', 1;
        END
        
        -- ============================================
        -- VALIDATION 2: Check if project exists
        -- ============================================
        SELECT @ProjectExists = 1
        FROM Projects
        WHERE ProjectId = @ProjectId;
        
        IF @ProjectExists = 0
        BEGIN
            THROW 50003, 'Project does not exist.', 1;
        END
        
        -- ============================================
        -- VALIDATION 3: Check if employee already assigned to this project
        -- ============================================
        SELECT @AlreadyAssigned = 1
        FROM EmployeeProjects
        WHERE EmployeeId = @EmployeeId 
            AND ProjectId = @ProjectId
            AND IsActive = 1;
        
        IF @AlreadyAssigned = 1
        BEGIN
            THROW 50004, 'Employee is already assigned to this project.', 1;
        END
        
        -- ============================================
        -- VALIDATION 4: Check if employee is assigned to 3 or more active projects
        -- ============================================
        SELECT @EmployeeActiveProjectCount = COUNT(*)
        FROM EmployeeProjects
        WHERE EmployeeId = @EmployeeId
            AND IsActive = 1;
        
        IF @EmployeeActiveProjectCount >= 3
        BEGIN
            THROW 50005, 'Employee is already assigned to 3 or more active projects. Cannot assign to more projects.', 1;
        END
        
        -- ============================================
        -- VALIDATION 5: Check if project has 10 or more active employees
        -- ============================================
        SELECT @ProjectActiveEmployeeCount = COUNT(*)
        FROM EmployeeProjects
        WHERE ProjectId = @ProjectId
            AND IsActive = 1;
        
        IF @ProjectActiveEmployeeCount >= 10
        BEGIN
            THROW 50006, 'Project already has 10 or more active employees. Cannot assign more employees.', 1;
        END
        
        -- ============================================
        -- ALL VALIDATIONS PASSED - Insert new assignment
        -- ============================================
        INSERT INTO EmployeeProjects (EmployeeId, ProjectId, IsActive)
        VALUES (@EmployeeId, @ProjectId, 1);
        
        -- Commit the transaction
        COMMIT TRANSACTION;
        
        -- Return success message
        PRINT 'SUCCESS: Employee has been successfully assigned to the project.';
        SELECT 
            'Success' AS Result,
            @EmployeeId AS EmployeeId,
            @ProjectId AS ProjectId,
            GETDATE() AS AssignedOn;
        
    END TRY
    BEGIN CATCH
        -- Rollback transaction on error
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        -- Capture error information
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        -- Display error message
        PRINT 'ERROR: ' + @ErrorMessage;
        
        -- Re-throw the error
        THROW;
        
    END CATCH
END
GO