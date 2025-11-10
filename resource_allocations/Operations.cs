using System;
using System.Data;
using System.Data.SqlClient;

namespace resource_allocations
{
    public static class Operations
    {
        private static readonly string connectionString =
            "Server=;Database=Resource_Allocations;Integrated Security=true;TrustServerCertificate=true;";
        private static readonly string storedProcedure = "AssignEmployeeToProject";
        public static void InsertSampleData()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {

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

                        SqlCommand cmd = new SqlCommand(insertEmployees, conn, transaction);
                        cmd.ExecuteNonQuery();

                        string[] projects = new string[]
                        {
                            "('E-Commerce Platform', '2024-01-01', '2024-12-31')",
                            "('Mobile Banking App', '2024-03-01', '2024-09-30')",
                            "('Cloud Migration', '2024-02-15', '2024-08-15')"
                        };

                        string insertProjects = "INSERT INTO Projects (ProjectName, StartDate, EndDate) VALUES " +
                            string.Join(", ", projects);
                        cmd = new SqlCommand(insertProjects, conn, transaction);

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
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(storedProcedure, conn) { CommandType = CommandType.StoredProcedure })
                    {

                        // Add parameters
                        cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                        cmd.Parameters.AddWithValue("@ProjectId", projectId);

                        // Execute stored procedure
                        cmd.ExecuteNonQuery();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ SUCCESS: Employee {employeeId} assigned to Project {projectId}");
                        Console.ResetColor();
                    }
                }
                catch (SqlException sqlEx)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ SQL ERROR: {sqlEx.Message}");
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
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        bool empExists = CheckEmployeeExists(employeeId);
                        if (!empExists) throw new Exception($"Employee with Id {employeeId} does not exist");


                        string updateEmpQuery = $@"
                            UPDATE Employees 
                            SET Status = 'Inactive' 
                            WHERE EmployeeId = {employeeId}";

                        using (SqlCommand cmd = new SqlCommand(updateEmpQuery, conn, transaction))
                        {
                            if (cmd.ExecuteNonQuery() == 0) throw new Exception($"Error updating employee ID {employeeId}");
                        }


                        string deactivateProjectsQuery = $@"
                            UPDATE EmployeeProjects
                            SET isActive = 0
                            WHERE EmployeeId = {employeeId}";

                        int projectsDeactivated;
                        using (SqlCommand cmd = new SqlCommand(deactivateProjectsQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                            projectsDeactivated = cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ SUCCESS: Employee with Id {employeeId} deactivated");
                        Console.WriteLine($"  - {projectsDeactivated} active project assignment(s) deactivated");
                        Console.ResetColor();
                    }
                    catch (SqlException sqlEx)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ SQL ERROR: {sqlEx.Message}");
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
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = $@"SELECT FullName FROM Employees WHERE EmployeeId = {employeeId}";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    object result = cmd.ExecuteScalar();
                    return (!(string.IsNullOrEmpty(result.ToString())) && !(string.IsNullOrWhiteSpace(result.ToString())));
                }
            }
        }

        public static void DisplayActiveEmployeeProjectsAssignments()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
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

                    using (SqlCommand cmd = new SqlCommand(dispQuery, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
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
                catch (SqlException sqlEx)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ SQL ERROR: {sqlEx.Message}");
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
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Check if employee exists and is inactive
                        string checkQuery = $@"
                            SELECT FullName, Status 
                            FROM Employees 
                            WHERE EmployeeId = {employeeId}";

                        string employeeName = "";
                        string status = "";

                        using (SqlCommand cmd = new SqlCommand(checkQuery, conn, transaction))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
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

                        using (SqlCommand cmd = new SqlCommand(deleteQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
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
    }
}