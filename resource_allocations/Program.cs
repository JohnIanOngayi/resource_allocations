using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace resource_allocations
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║     RESOURCE ALLOCATION MANAGEMENT SYSTEM                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // Part 4: Insert sample data
                Console.WriteLine("=== PART 4: INSERTING SAMPLE DATA ===\n");
                MySqlOperations.InsertSampleData();

                // Demonstrate CREATE operation - Successful scenarios
                Console.WriteLine("\n=== CREATE: Testing Successful Assignments ===\n");
                MySqlOperations.TestSuccessfulAssignment();

                // Demonstrate READ operation
                Console.WriteLine("\n=== READ: Display All Active Assignments ===\n");
                MySqlOperations.DisplayActiveEmployeeProjectsAssignments();

                // Demonstrate CREATE operation - Failed scenarios
                Console.WriteLine("\n=== CREATE: Testing Failed Assignments (Validations) ===\n");
                MySqlOperations.TestFailedAssignments();

                // Demonstrate UPDATE operation
                Console.WriteLine("\n=== UPDATE: Deactivate Employee and Their Projects ===\n");
                MySqlOperations.TestDeactivateEmployee();

                // Demonstrate DELETE operation (Optional)
                Console.WriteLine("\n=== DELETE: Remove Inactive Employee ===\n");
                MySqlOperations.TestDeleteInactiveEmployee();

                // Final state
                Console.WriteLine("\n=== FINAL STATE: All Active Assignments ===\n");
                MySqlOperations.DisplayActiveEmployeeProjectsAssignments();

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL ERROR] {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
