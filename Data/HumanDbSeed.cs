using CeoMemo.Models.Human;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Data
{
    public static class HumanDbSeed
    {
        public static void Initialize(HumanDbContext context)
        {
            // Only run if databases are empty
            if (context.Departments.Any() || context.Positions.Any() || context.Employees.Any())
            {
                return; // Database has already been seeded
            }

            try
            {
                // Seed Departments without identity insert
                var departments = new List<Department>
                {
                    new Department { DepartmentName = "Human Resources", CreatedAt = DateTime.Now },
                    new Department { DepartmentName = "IT Department", CreatedAt = DateTime.Now },
                    new Department { DepartmentName = "Finance", CreatedAt = DateTime.Now },
                    new Department { DepartmentName = "Marketing", CreatedAt = DateTime.Now },
                    new Department { DepartmentName = "Operations", CreatedAt = DateTime.Now }
                };
                context.Departments.AddRange(departments);
                context.SaveChanges();
                
                // Seed Positions
                var positions = new List<Position>
                {
                    new Position { PositionName = "Manager", CreatedAt = DateTime.Now },
                    new Position { PositionName = "Senior Developer", CreatedAt = DateTime.Now },
                    new Position { PositionName = "Junior Developer", CreatedAt = DateTime.Now },
                    new Position { PositionName = "HR Specialist", CreatedAt = DateTime.Now },
                    new Position { PositionName = "Financial Analyst", CreatedAt = DateTime.Now },
                    new Position { PositionName = "Marketing Specialist", CreatedAt = DateTime.Now }
                };
                context.Positions.AddRange(positions);
                context.SaveChanges();
                
                // Seed Users
                var users = new List<User>
                {
                    new User { 
                        Username = "admin", 
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), 
                        Role = "Admin", 
                        CreatedAt = DateTime.Now, 
                        UpdatedAt = DateTime.Now 
                    },
                    new User { 
                        Username = "hr_manager", 
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("hr123"), 
                        Role = "HRManager", 
                        CreatedAt = DateTime.Now, 
                        UpdatedAt = DateTime.Now 
                    },
                    new User { 
                        Username = "payroll_manager", 
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("payroll123"), 
                        Role = "PayrollManager", 
                        CreatedAt = DateTime.Now, 
                        UpdatedAt = DateTime.Now 
                    },
                    new User { 
                        Username = "employee1", 
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("emp123"), 
                        Role = "Employee", 
                        CreatedAt = DateTime.Now, 
                        UpdatedAt = DateTime.Now 
                    }
                };
                context.Users.AddRange(users);
                context.SaveChanges();
                
                // Get department and position IDs
                var departmentsList = context.Departments.ToList();
                var positionsList = context.Positions.ToList();
                
                // Seed Employees - use the IDs from the database
                var employees = new List<Employee>
                {
                    new Employee { 
                        FullName = "John Doe", 
                        DateOfBirth = new DateOnly(1985, 5, 15), 
                        Gender = "Male", 
                        PhoneNumber = "0901234567", 
                        Email = "john.doe@example.com", 
                        HireDate = new DateOnly(2020, 1, 10), 
                        DepartmentId = departmentsList[1].DepartmentId, // IT Department
                        PositionId = positionsList[0].PositionId, // Manager
                        Status = "Active", 
                        CreatedAt = DateTime.Now 
                    },
                    new Employee { 
                        FullName = "Jane Smith", 
                        DateOfBirth = new DateOnly(1990, 8, 22), 
                        Gender = "Female", 
                        PhoneNumber = "0987654321", 
                        Email = "jane.smith@example.com", 
                        HireDate = new DateOnly(2021, 3, 15), 
                        DepartmentId = departmentsList[1].DepartmentId, // IT Department
                        PositionId = positionsList[1].PositionId, // Senior Developer
                        Status = "Active", 
                        CreatedAt = DateTime.Now 
                    },
                    new Employee { 
                        FullName = "Robert Johnson", 
                        DateOfBirth = new DateOnly(1992, 2, 18), 
                        Gender = "Male", 
                        PhoneNumber = "0912345678", 
                        Email = "robert.j@example.com", 
                        HireDate = new DateOnly(2021, 6, 1), 
                        DepartmentId = departmentsList[2].DepartmentId, // Finance
                        PositionId = positionsList[4].PositionId, // Financial Analyst
                        Status = "Active", 
                        CreatedAt = DateTime.Now 
                    },
                    new Employee { 
                        FullName = "Emily Davis", 
                        DateOfBirth = new DateOnly(1988, 11, 7), 
                        Gender = "Female", 
                        PhoneNumber = "0923456789", 
                        Email = "emily.d@example.com", 
                        HireDate = new DateOnly(2019, 8, 12), 
                        DepartmentId = departmentsList[0].DepartmentId, // Human Resources
                        PositionId = positionsList[3].PositionId, // HR Specialist
                        Status = "Active", 
                        CreatedAt = DateTime.Now 
                    },
                    new Employee { 
                        FullName = "Michael Brown", 
                        DateOfBirth = new DateOnly(1995, 4, 30), 
                        Gender = "Male", 
                        PhoneNumber = "0934567890", 
                        Email = "michael.b@example.com", 
                        HireDate = new DateOnly(2022, 1, 15), 
                        DepartmentId = departmentsList[3].DepartmentId, // Marketing
                        PositionId = positionsList[5].PositionId, // Marketing Specialist
                        Status = "Active", 
                        CreatedAt = DateTime.Now 
                    }
                };
                context.Employees.AddRange(employees);
                context.SaveChanges();
                
                // Get employee IDs for dividends
                var employeesList = context.Employees.ToList();
                
                // Seed Dividends
                var dividends = new List<Dividend>
                {
                    new Dividend { 
                        EmployeeId = employeesList[0].EmployeeId, // John Doe
                        DividendAmount = 5000.00m, 
                        DividendDate = new DateOnly(2023, 12, 15), 
                        CreatedAt = DateTime.Now 
                    },
                    new Dividend { 
                        EmployeeId = employeesList[1].EmployeeId, // Jane Smith
                        DividendAmount = 3000.00m, 
                        DividendDate = new DateOnly(2023, 12, 15), 
                        CreatedAt = DateTime.Now 
                    },
                    new Dividend { 
                        EmployeeId = employeesList[2].EmployeeId, // Robert Johnson
                        DividendAmount = 2500.00m, 
                        DividendDate = new DateOnly(2023, 12, 15), 
                        CreatedAt = DateTime.Now 
                    }
                };
                context.Dividends.AddRange(dividends);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log the exception details or rethrow it
                throw new Exception("Error seeding human database", ex);
            }
        }
    }
}
