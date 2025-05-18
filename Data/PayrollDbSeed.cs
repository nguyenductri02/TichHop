using CeoMemo.Models.Payroll;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Data
{
    public static class PayrollDbSeed
    {
        public static void Initialize(PayrollDbContext context)
        {
            // Only run if databases are empty
            if (context.Departments.Any() || context.Positions.Any() || context.Employees.Any())
            {
                return; // Database has already been seeded
            }

            try
            {
                // Fix the issue with the ValueGeneratedNever() constraint in OnModelCreating
                // We need to temporarily modify the model configuration to allow auto-generated IDs
                context.Database.ExecuteSqlRaw("ALTER TABLE departments MODIFY COLUMN DepartmentID INT AUTO_INCREMENT");
                context.Database.ExecuteSqlRaw("ALTER TABLE positions MODIFY COLUMN PositionID INT AUTO_INCREMENT");
                context.Database.ExecuteSqlRaw("ALTER TABLE employees MODIFY COLUMN EmployeeID INT AUTO_INCREMENT");
                
                // Seed Departments - don't specify IDs to let MySQL generate them
                var departments = new List<Department>
                {
                    new Department { DepartmentName = "Human Resources" },
                    new Department { DepartmentName = "IT Department" },
                    new Department { DepartmentName = "Finance" },
                    new Department { DepartmentName = "Marketing" },
                    new Department { DepartmentName = "Operations" }
                };
                
                foreach (var dept in departments)
                {
                    context.Departments.Add(dept);
                    context.SaveChanges();
                }
                
                // Seed Positions - don't specify IDs to let MySQL generate them
                var positions = new List<Position>
                {
                    new Position { PositionName = "Manager" },
                    new Position { PositionName = "Senior Developer" },
                    new Position { PositionName = "Junior Developer" },
                    new Position { PositionName = "HR Specialist" },
                    new Position { PositionName = "Financial Analyst" },
                    new Position { PositionName = "Marketing Specialist" }
                };
                
                foreach (var pos in positions)
                {
                    context.Positions.Add(pos);
                    context.SaveChanges();
                }
                
                // Get the newly created department and position IDs
                var departmentsList = context.Departments.ToList();
                var positionsList = context.Positions.ToList();
                
                // Seed Employees - use the matching name pattern to find the right department/position
                // We'll seed employees one by one to ensure proper ID assignment
                var employeesToAdd = new List<(string Name, string DeptName, string PosName)>
                {
                    ("John Doe", "IT Department", "Manager"),
                    ("Jane Smith", "IT Department", "Senior Developer"),
                    ("Robert Johnson", "Finance", "Financial Analyst"),
                    ("Emily Davis", "Human Resources", "HR Specialist"),
                    ("Michael Brown", "Marketing", "Marketing Specialist")
                };
                
                var employees = new List<Employee>();
                
                foreach (var emp in employeesToAdd)
                {
                    var dept = departmentsList.FirstOrDefault(d => d.DepartmentName == emp.DeptName);
                    var pos = positionsList.FirstOrDefault(p => p.PositionName == emp.PosName);
                    
                    if (dept != null && pos != null)
                    {
                        var employee = new Employee
                        {
                            FullName = emp.Name,
                            DepartmentId = dept.DepartmentId,
                            PositionId = pos.PositionId,
                            Status = "Active"
                        };
                        
                        context.Employees.Add(employee);
                        context.SaveChanges();
                        employees.Add(employee);
                    }
                }
                
                // Seed Salaries
                var currentMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);
                var lastMonth = currentMonth.AddMonths(-1);
                
                var salaryData = new List<(int EmployeeIndex, DateOnly Month, decimal BaseSalary, decimal Bonus, decimal Deductions)>
                {
                    (0, currentMonth, 5000.00m, 1000.00m, 500.00m), // John Doe, current month
                    (1, currentMonth, 4000.00m, 500.00m, 400.00m),  // Jane Smith, current month
                    (2, currentMonth, 3500.00m, 300.00m, 350.00m),  // Robert Johnson, current month
                    (3, currentMonth, 3800.00m, 400.00m, 380.00m),  // Emily Davis, current month
                    (4, currentMonth, 3600.00m, 300.00m, 360.00m),  // Michael Brown, current month
                    (0, lastMonth, 5000.00m, 800.00m, 500.00m),     // John Doe, last month
                    (1, lastMonth, 4000.00m, 400.00m, 400.00m)      // Jane Smith, last month
                };
                
                foreach (var salaryItem in salaryData)
                {
                    if (salaryItem.EmployeeIndex < employees.Count)
                    {
                        var employee = employees[salaryItem.EmployeeIndex];
                        var netSalary = salaryItem.BaseSalary + salaryItem.Bonus - salaryItem.Deductions;
                        
                        var salary = new Salary
                        {
                            EmployeeId = employee.EmployeeId,
                            SalaryMonth = salaryItem.Month,
                            BaseSalary = salaryItem.BaseSalary,
                            Bonus = salaryItem.Bonus,
                            Deductions = salaryItem.Deductions,
                            NetSalary = netSalary,
                            CreatedAt = salaryItem.Month == currentMonth ? DateTime.Now : DateTime.Now.AddMonths(-1)
                        };
                        
                        context.Salaries.Add(salary);
                        context.SaveChanges();
                    }
                }
                
                // Seed Attendance
                var attendanceData = new List<(int EmployeeIndex, DateOnly Month, int WorkDays, int AbsentDays, int LeaveDays)>
                {
                    (0, currentMonth, 22, 0, 0), // John Doe, current month
                    (1, currentMonth, 20, 0, 2), // Jane Smith, current month
                    (2, currentMonth, 21, 1, 0), // Robert Johnson, current month
                    (3, currentMonth, 22, 0, 0), // Emily Davis, current month
                    (4, currentMonth, 19, 1, 2), // Michael Brown, current month
                    (0, lastMonth, 21, 0, 1),    // John Doe, last month
                    (1, lastMonth, 22, 0, 0)     // Jane Smith, last month
                };
                
                foreach (var attendanceItem in attendanceData)
                {
                    if (attendanceItem.EmployeeIndex < employees.Count)
                    {
                        var employee = employees[attendanceItem.EmployeeIndex];
                        
                        var attendance = new Attendance
                        {
                            EmployeeId = employee.EmployeeId,
                            AttendanceMonth = attendanceItem.Month,
                            WorkDays = attendanceItem.WorkDays,
                            AbsentDays = attendanceItem.AbsentDays,
                            LeaveDays = attendanceItem.LeaveDays,
                            CreatedAt = attendanceItem.Month == currentMonth ? DateTime.Now : DateTime.Now.AddMonths(-1)
                        };
                        
                        context.Attendances.Add(attendance);
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception details or rethrow it
                throw new Exception("Error seeding payroll database", ex);
            }
        }
    }
}
