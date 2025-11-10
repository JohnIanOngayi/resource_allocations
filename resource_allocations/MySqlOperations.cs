using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace resource_allocations
{
    public static class MySqlOperations
    {
        private static readonly string connectionString =
            @"Server=127.0.0.1;Port=3306;Database=Resource_Allocations;Uid=root;Pwd=;";
        private static readonly string storedProcedure = "AssignEmployeeToProject";
        public static void InsertSampleData()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        MySqlCommand cmd = new MySqlCommand("DELETE FROM EmployeeProjects", conn, transaction);
                        cmd.ExecuteNonQuery();

                        cmd = new MySqlCommand("DELETE FROM Projects", conn, transaction);
                        cmd.ExecuteNonQuery();

                        cmd = new MySqlCommand("DELETE FROM Employees", conn, transaction);
                        cmd.ExecuteNonQuery();

                        // Insert 5 employees
                        string[] employees = new string[]
                        {
                            "('Alice Johnson', 'alice.johnson@company.com', 'Active')",
                            "('Bob Smith', 'bob.smith@company.com', 'Active')",
                            "('Charlie Brown', 'charlie.brown@company.com', 'Active')",
                            "('Diana Prince', 'diana.prince@company.com', 'Active')",
                            "('Eve Davis', 'eve.davis@company.com', 'Inactive')"
                        };

                        string insertEmployees = "INSERT INTO Employees (FullName, Email, Status) VALUES " +
                            string.Join(", ", employees);

                        cmd = new MySqlCommand(insertEmployees, conn, transaction);
                        cmd.ExecuteNonQuery();

                        string[] projects = new string[]
                        {   
                            "('E-Commerce Platform', '2024-01-01', '2024-12-31')",
                            "('Mobile Banking App', '2024-03-01', '2024-09-30')",
                            "('Cloud Migration', '2024-02-15', '2024-08-15')"
                        };

                        string insertProjects = "INSERT INTO Projects (ProjectName, StartDate, EndDate) VALUES " +
                            string.Join(", ", projects);
                        cmd = new MySqlCommand(insertProjects, conn, transaction);
                        cmd.ExecuteNonQuery();

                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ Sample data inserted successfully!");
                        Console.WriteLine("  - 5 Employees added");
                        Console.WriteLine("  - 3 Projects added");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Failed to insert sample data: {ex.Message}");
                    }
                }
            }
        }

        public static void AssignEmployeeToProject(int employeeId, int projectId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    using (MySqlCommand cmd = new MySqlCommand(storedProcedure, conn) { CommandType = CommandType.StoredProcedure })
                    {

                        // Add parameters
                        cmd.Parameters.AddWithValue("p_EmployeeId", employeeId);
                        cmd.Parameters.AddWithValue("p_ProjectId", projectId);

                        // Execute stored procedure
                        cmd.ExecuteNonQuery();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ SUCCESS: Employee {employeeId} assigned to Project {projectId}");
                        Console.ResetColor();
                    }
                }
                catch (MySqlException sqlEx)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ MySql ERROR: {sqlEx.Message}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ ERROR: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
        public static void DeactivateEmployee(int employeeId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        bool empExists = CheckEmployeeExists(employeeId);
                        if (!empExists) throw new Exception($"Employee with Id {employeeId} does not exist");


                        string updateEmpQuery = @"
                            UPDATE Employees 
                            SET Status = 'Inactive' 
                            WHERE EmployeeId = @EmployeeId";

                        using (MySqlCommand cmd = new MySqlCommand(updateEmpQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("EmployeeId", employeeId);
                            if (cmd.ExecuteNonQuery() == 0) throw new Exception($"Error updating employee ID {employeeId}");
                        }


                        string deactivateProjectsQuery = @"
                            UPDATE EmployeeProjects
                            SET isActive = 0
                            WHERE EmployeeId = @EmployeeId";

                        int projectsDeactivated;
                        using (MySqlCommand cmd = new MySqlCommand(deactivateProjectsQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("EmployeeId", employeeId);
                            projectsDeactivated = cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ SUCCESS: Employee with Id {employeeId} deactivated");
                        Console.WriteLine($"  - {projectsDeactivated} active project assignment(s) deactivated");
                        Console.ResetColor();
                    }
                    catch (MySqlException sqlEx)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ MySql ERROR: {sqlEx.Message}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ ERROR: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }

        public static bool CheckEmployeeExists(int employeeId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT FullName FROM Employees WHERE EmployeeId = @EmployeeId";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("EmployeeId", employeeId);
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value;
                }
            }
        }

        public static void DisplayActiveEmployeeProjectsAssignments()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                try
                {
                    string dispQuery = @"
                        SELECT 
                            e.EmployeeId,
                            e.FullName,
                            e.Status,
                            p.ProjectId,
                            p.ProjectName,
                            ep.AssignedOn,
                            ep.IsActive
                        FROM Employees e
                        INNER JOIN EmployeeProjects ep ON e.EmployeeId = ep.EmployeeId
                        INNER JOIN Projects p ON ep.ProjectId = p.ProjectId
                        WHERE ep.IsActive = 1
                        ORDER BY e.FullName, p.ProjectName";

                    using (MySqlCommand cmd = new MySqlCommand(dispQuery, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("{0,-5} {1,-20} {2,-10} {3,-5} {4,-25} {5,-20}",
                                "EmpID", "Employee Name", "Status", "PrjID", "Project Name", "Assigned On");
                            Console.WriteLine(new string('-', 95));

                            int count = 0;
                            while (reader.Read())
                            {
                                Console.WriteLine("{0,-5} {1,-20} {2,-10} {3,-5} {4,-25} {5,-20}",
                                    reader["EmployeeId"],
                                    reader["FullName"],
                                    reader["Status"],
                                    reader["ProjectId"],
                                    reader["ProjectName"],
                                    ((DateTime)reader["AssignedOn"]).ToString("yyyy-MM-dd HH:mm"));
                                count++;
                            }

                            if (count == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("No active assignments found.");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.WriteLine(new string('-', 95));
                                Console.WriteLine($"Total Active Assignments: {count}");
                            }
                        }
                    }
                }
                catch (MySqlException sqlEx)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ MySql ERROR: {sqlEx.Message}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ ERROR: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        public static void DeleteInactiveEmployee(int employeeId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Check if employee exists and is inactive
                        string checkQuery = @"
                            SELECT FullName, Status 
                            FROM Employees
                            WHERE employeeId = @EmployeeId";

                        string employeeName = "";
                        string status = "";

                        using (MySqlCommand cmd = new MySqlCommand(checkQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("EmployeeId", employeeId);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    employeeName = reader["FullName"].ToString();
                                    status = reader["Status"].ToString();
                                }
                                else
                                {
                                    throw new Exception($"Employee with ID {employeeId} does not exist.");
                                }
                            }
                        }

                        if (status == "Active")
                        {
                            throw new Exception($"Cannot delete active employee '{employeeName}'. Deactivate first.");
                        }

                        // Delete employee (CASCADE will delete EmployeeProjects)
                        string deleteQuery = "DELETE FROM Employees WHERE EmployeeId = @EmployeeId";

                        using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("EmployeeId", employeeId);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                transaction.Commit();
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"✓ SUCCESS: Inactive employee '{employeeName}' (ID:{employeeId}) deleted");
                                Console.ResetColor();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ ERROR: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }

        public static void TestSuccessfulAssignment()
        {
            Console.WriteLine("Test 1: Assign Alice (ID:1) to E-Commerce Platform (ID:1)");
            AssignEmployeeToProject(1, 1);

            Console.WriteLine("\nTest 2: Assign Bob (ID:2) to E-Commerce Platform (ID:1)");
            AssignEmployeeToProject(2, 1);

            Console.WriteLine("\nTest 3: Assign Alice (ID:1) to Mobile Banking App (ID:2)");
            AssignEmployeeToProject(1, 2);
        }

        public static void TestFailedAssignments()
        {
            Console.WriteLine("Test 1: Non-existent Employee (ID:999)");
            AssignEmployeeToProject(999, 1);

            Console.WriteLine("\nTest 2: Inactive Employee (Eve - ID:5)");
            AssignEmployeeToProject(5, 1);

            Console.WriteLine("\nTest 3: Non-existent Project (ID:999)");
            AssignEmployeeToProject(1, 999);

            Console.WriteLine("\nTest 4: Duplicate Assignment (Alice already on Project 1)");
            AssignEmployeeToProject(1, 1);

            Console.WriteLine("\nTest 5: Employee with 3+ Active Projects");
            // First assign Alice to project 3 (she already has 2)
            AssignEmployeeToProject(1, 3);
            // Try to assign to a 4th project - should fail
            Console.WriteLine("Attempting to assign Alice to a 4th project (should fail):");
            AssignEmployeeToProject(1, 4); // This will fail - no project 4 exists anyway

            Console.WriteLine("\nTest 6: Project with 10+ Active Employees");
            Console.WriteLine("Assigning multiple employees to Project 2...");
            for (int i = 2; i <= 4; i++)
            {
                AssignEmployeeToProject(i, 2);
            }
        }
        public static void TestDeactivateEmployee()
        {
            Console.WriteLine("Deactivating Bob Smith (ID:2) and all his active projects...\n");
            DeactivateEmployee(2);

            Console.WriteLine("\nAttempting to deactivate non-existent employee (ID:999)...\n");
            DeactivateEmployee(999);
        }

        public static void TestDeleteInactiveEmployee()
        {
            Console.WriteLine("Attempting to delete inactive employee Eve Davis (ID:5)...\n");
            DeleteInactiveEmployee(5);

            Console.WriteLine("\nAttempting to delete active employee Alice (ID:1) - should fail...\n");
            DeleteInactiveEmployee(1);
        }
    }
}