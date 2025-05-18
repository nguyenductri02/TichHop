using CeoMemo.Models.Human;
using CeoMemo.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CeoMemo.Data
{
    public class DbSeeder
    {
        private readonly HumanDbContext _humanContext;
        private readonly PayrollDbContext _payrollContext;

        public DbSeeder(HumanDbContext humanContext, PayrollDbContext payrollContext)
        {
            _humanContext = humanContext;
            _payrollContext = payrollContext;
        }

        public async Task SeedAsync()
        {
            // Make sure database is created
            await _humanContext.Database.EnsureCreatedAsync();
            await _payrollContext.Database.EnsureCreatedAsync();

            // Seed users for login
            await SeedUsersAsync();

            // Seed departments
            await SeedDepartmentsAsync();

            // Seed positions
            await SeedPositionsAsync();

            // Seed employees
            await SeedEmployeesAsync();

            // Seed payroll data
            await SeedPayrollDataAsync();
        }

        private async Task SeedUsersAsync()
        {
            if (!await _humanContext.Users.AnyAsync())
            {
                var users = new[]
                {
                    new User
                    {
                        Username = "admin",
                        Email = "admin@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Password: admin123
                        Role = "Administrator",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = "Active"
                    },
                    new User
                    {
                        Username = "hr",
                        Email = "hr@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("hr123"), // Password: hr123
                        Role = "HR Manager",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = "Active"
                    },
                    new User
                    {
                        Username = "finance",
                        Email = "finance@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("finance123"), // Password: finance123
                        Role = "Payroll Manager",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = "Active"
                    }
                };

                await _humanContext.Users.AddRangeAsync(users);
                await _humanContext.SaveChangesAsync();

                Console.WriteLine("Users seeded successfully!");
                Console.WriteLine("Login credentials:");
                Console.WriteLine("Administrator - Username: admin, Password: admin123");
                Console.WriteLine("HR Manager - Username: hr, Password: hr123");
                Console.WriteLine("Payroll Manager - Username: finance, Password: finance123");
            }
        }

        private async Task SeedDepartmentsAsync()
        {
            if (!await _humanContext.Departments.AnyAsync())
            {
                var departments = new[]
                {
                    new CeoMemo.Models.Human.Department { Name = "IT", Manager = "John Smith", Description = "Information Technology Department", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Department { Name = "HR", Manager = "Jane Doe", Description = "Human Resources Department", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Department { Name = "Finance", Manager = "Robert Johnson", Description = "Finance and Accounting Department", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Department { Name = "Marketing", Manager = "Emily Wilson", Description = "Marketing and Sales Department", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Department { Name = "Operations", Manager = "Michael Brown", Description = "Operations Department", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                };

                await _humanContext.Departments.AddRangeAsync(departments);
                await _humanContext.SaveChangesAsync();

                // Also add departments to PayrollDb
                foreach (var dept in departments)
                {
                    var payrollDept = new CeoMemo.Models.Payroll.Department
                    {
                        DepartmentId = dept.DepartmentId,
                        Name = dept.Name
                    };
                    await _payrollContext.Departments.AddAsync(payrollDept);
                }
                await _payrollContext.SaveChangesAsync();

                Console.WriteLine("Departments seeded successfully in both databases!");
            }
        }

        private async Task SeedPositionsAsync()
        {
            if (!await _humanContext.Positions.AnyAsync())
            {
                var positions = new[]
                {
                    new CeoMemo.Models.Human.Position { Name = "Software Developer", DepartmentId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "HR Manager", DepartmentId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "Accountant", DepartmentId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "Marketing Specialist", DepartmentId = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "Operations Manager", DepartmentId = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "System Administrator", DepartmentId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "HR Assistant", DepartmentId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "Financial Analyst", DepartmentId = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "Content Creator", DepartmentId = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new CeoMemo.Models.Human.Position { Name = "Logistics Coordinator", DepartmentId = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                };

                await _humanContext.Positions.AddRangeAsync(positions);
                await _humanContext.SaveChangesAsync();

                Console.WriteLine("Positions seeded successfully!");
            }
        }

        private async Task SeedEmployeesAsync()
        {
            if (!await _humanContext.Employees.AnyAsync())
            {
                var employees = new[]
                {
                    new CeoMemo.Models.Human.Employee 
                    { 
                        FullName = "John Doe", 
                        DateOfBirth = new DateOnly(1985, 5, 15), 
                        Gender = "Male", 
                        PhoneNumber = "+1234567890", 
                        Email = "john.doe@example.com", 
                        HireDate = new DateOnly(2020, 1, 15), 
                        DepartmentId = 1, 
                        PositionId = 1, 
                        Status = "Active", 
                        CreatedAt = DateTime.UtcNow, 
                        UpdatedAt = DateTime.UtcNow 
                    },
                    new CeoMemo.Models.Human.Employee 
                    { 
                        FullName = "Jane Smith", 
                        DateOfBirth = new DateOnly(1990, 8, 22), 
                        Gender = "Female", 
                        PhoneNumber = "+0987654321", 
                        Email = "jane.smith@example.com", 
                        HireDate = new DateOnly(2019, 3, 10), 
                        DepartmentId = 2, 
                        PositionId = 2, 
                        Status = "Active", 
                        CreatedAt = DateTime.UtcNow, 
                        UpdatedAt = DateTime.UtcNow 
                    },
                    new CeoMemo.Models.Human.Employee 
                    { 
                        FullName = "Robert Johnson", 
                        DateOfBirth = new DateOnly(1988, 11, 30), 
                        Gender = "Male", 
                        PhoneNumber = "+1122334455", 
                        Email = "robert.johnson@example.com", 
                        HireDate = new DateOnly(2018, 6, 5), 
                        DepartmentId = 3, 
                        PositionId = 3, 
                        Status = "Active", 
                        CreatedAt = DateTime.UtcNow, 
                        UpdatedAt = DateTime.UtcNow 
                    },
                    new CeoMemo.Models.Human.Employee 
                    { 
                        FullName = "Emily Wilson", 
                        DateOfBirth = new DateOnly(1992, 4, 12), 
                        Gender = "Female", 
                        PhoneNumber = "+5566778899", 
                        Email = "emily.wilson@example.com", 
                        HireDate = new DateOnly(2021, 2, 20), 
                        DepartmentId = 4, 
                        PositionId = 4, 
                        Status = "Active", 
                        CreatedAt = DateTime.UtcNow, 
                        UpdatedAt = DateTime.UtcNow 
                    },
                    new CeoMemo.Models.Human.Employee 
                    { 
                        FullName = "Michael Brown", 
                        DateOfBirth = new DateOnly(1982, 7, 8), 
                        Gender = "Male", 
                        PhoneNumber = "+9988776655", 
                        Email = "michael.brown@example.com", 
                        HireDate = new DateOnly(2017, 9, 15), 
                        DepartmentId = 5, 
                        PositionId = 5, 
                        Status = "Active", 
                        CreatedAt = DateTime.UtcNow, 
                        UpdatedAt = DateTime.UtcNow 
                    }
                };

                await _humanContext.Employees.AddRangeAsync(employees);
                await _humanContext.SaveChangesAsync();

                Console.WriteLine("Employees seeded in Human DB successfully!");

                // Now add these employees to Payroll DB as well
                foreach (var emp in employees)
                {
                    var payrollEmployee = new CeoMemo.Models.Payroll.Employee
                    {
                        EmployeeId = emp.EmployeeId,
                        FullName = emp.FullName,
                        DepartmentId = emp.DepartmentId ?? 0,
                        PositionId = emp.PositionId ?? 0,
                        Status = emp.Status
                    };

                    await _payrollContext.Employees.AddAsync(payrollEmployee);
                }

                await _payrollContext.SaveChangesAsync();
                Console.WriteLine("Employees seeded in Payroll DB successfully!");
            }
        }

        private async Task SeedPayrollDataAsync()
        {
            if (!await _payrollContext.Salaries.AnyAsync())
            {
                // Get all employees
                var employees = await _humanContext.Employees.ToListAsync();
                var currentDate = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var lastMonth = currentDate.AddMonths(-1);

                foreach (var emp in employees)
                {
                    // Create salary for last month
                    var baseSalary = 3000M + (decimal)((new Random()).NextDouble() * 2000);
                    var bonus = (decimal)((new Random()).NextDouble() * 500);
                    var deductions = (decimal)((new Random()).NextDouble() * 300);
                    var netSalary = baseSalary + bonus - deductions;

                    var salary = new CeoMemo.Models.Payroll.Salary
                    {
                        EmployeeId = emp.EmployeeId,
                        SalaryMonth = lastMonth,
                        BaseSalary = baseSalary,
                        Bonus = bonus,
                        Deductions = deductions,
                        NetSalary = netSalary
                    };

                    await _payrollContext.Salaries.AddAsync(salary);
                }

                await _payrollContext.SaveChangesAsync();
                Console.WriteLine("Salary data seeded successfully!");
            }
        }
    }
}
