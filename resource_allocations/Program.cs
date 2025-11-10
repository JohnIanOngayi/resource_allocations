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
                Operations.InsertSampleData();

                // Demonstrate READ operation
                Console.WriteLine("\n=== READ: Display All Active Assignments ===\n");
                Operations.DisplayActiveEmployeeProjectsAssignments();

                // Demonstrate CREATE operation - Successful scenarios
                Console.WriteLine("\n=== CREATE: Testing Successful Assignments ===\n");
                Operations.TestSuccessfulAssignment();

                // Demonstrate CREATE operation - Failed scenarios
                Console.WriteLine("\n=== CREATE: Testing Failed Assignments (Validations) ===\n");
                Operations.TestFailedAssignments();

                // Demonstrate UPDATE operation
                Console.WriteLine("\n=== UPDATE: Deactivate Employee and Their Projects ===\n");
                Operations.TestDeactivateEmployee();

                // Demonstrate DELETE operation (Optional)
                Console.WriteLine("\n=== DELETE: Remove Inactive Employee ===\n");
                Operations.TestDeleteInactiveEmployee();

                // Final state
                Console.WriteLine("\n=== FINAL STATE: All Active Assignments ===\n");
                Operations.DisplayActiveEmployeeProjectsAssignments();

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
