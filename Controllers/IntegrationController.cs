﻿using CeoMemo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CeoMemo.Models.Human;
using CeoMemo.Models.Payroll;
using CeoMemo.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YourNamespace.Controllers
{
    public class DepartmentDto { public int DepartmentId { get; set; } public string Name { get; set; } public string ManagerName { get; set; } }
    public class PositionDto { public int PositionId { get; set; } public string Name { get; set; } public string DepartmentName { get; set; } }
    public class NotificationDto { public int Id { get; set; } public string Message { get; set; } public DateTime Date { get; set; } public string Status { get; set; } public string Icon { get; set; } public string Color { get; set; } }
    public class ReportDataDto { public List<object> Employees { get; set; } public List<DepartmentDto> Departments { get; set; } }

    [ApiController]
    [Route("api/[controller]")]
    public class IntegrationController : ControllerBase
    {
        private readonly HumanDbContext _humanDb;
        private readonly PayrollDbContext _payrollDb;

        public IntegrationController(HumanDbContext humanDb, PayrollDbContext payrollDb)
        {
            _humanDb = humanDb;
            _payrollDb = payrollDb;
        }

        // GET /employees
        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sortBy = "EmployeeId", [FromQuery] bool sortDesc = false)
        {
            var query = _humanDb.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .AsQueryable();

            query = sortBy switch
            {
                "FullName" => sortDesc ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
                "HireDate" => sortDesc ? query.OrderByDescending(e => e.HireDate) : query.OrderBy(e => e.HireDate),
                _ => sortDesc ? query.OrderByDescending(e => e.EmployeeId) : query.OrderBy(e => e.EmployeeId)
            };

            var total = await query.CountAsync();
            var employees = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployeeDto
                {
                    FullName = e.FullName,
                    DateOfBirth = e.DateOfBirth,
                    Gender = e.Gender,
                    PhoneNumber = e.PhoneNumber,
                    Email = e.Email,
                    HireDate = e.HireDate,
                    DepartmentId = e.DepartmentId ?? 0,
                    PositionId = e.PositionId ?? 0,
                    Status = e.Status,
                    CreatedAt = (DateTime)e.CreatedAt,
                    UpdatedAt = (DateTime)e.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { Total = total, Employees = employees });
        }

        // GET /payroll
        [HttpGet("payroll")]
        public async Task<IActionResult> GetPayroll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var salaries = await _payrollDb.Salaries
                .OrderBy(s => s.SalaryId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var employeeIds = salaries.Select(s => s.EmployeeId).Distinct().ToList();

            var employees = await _humanDb.Employees
                .Where(e => employeeIds.Contains(e.EmployeeId))
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FullName,
                    DepartmentId = e.DepartmentId ?? 0,
                    PositionId = e.PositionId ?? 0
                })
                .ToListAsync();

            var result = from s in salaries
                         join e in employees on s.EmployeeId equals e.EmployeeId
                         select new SalaryDto
                         {
                             SalaryId = s.SalaryId,
                             SalaryMonth = s.SalaryMonth,
                             BaseSalary = s.BaseSalary,
                             Bonus = (decimal)s.Bonus,
                             Deductions = (decimal)s.Deductions,
                             NetSalary = s.NetSalary,
                             EmployeeID = e.EmployeeId,
                             FullName = e.FullName,
                             DepartmentID = e.DepartmentId,
                             PositionID = e.PositionId
                         };

            var totalSalaries = await _payrollDb.Salaries.CountAsync();
            var pagedResultList = result.ToList();

            return Ok(new { Salaries = pagedResultList, Total = totalSalaries });
        }

        // GET /attendance
        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendance()
        {
            var attendance = await _payrollDb.Attendances.ToListAsync();
            return Ok(attendance);
        }

        // POST /add-employee
        [HttpPost("add-employee")]
        public async Task<IActionResult> AddEmployee([FromBody] EmployeeDto dto)
        {
            if (!IsValidEmployeeDto(dto, out var errorMessage))
                return BadRequest(errorMessage);

            if (!await _humanDb.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId))
                return BadRequest("Phòng ban không tồn tại");
            if (!await _humanDb.Positions.AnyAsync(p => p.PositionId == dto.PositionId))
                return BadRequest("Chức vụ không tồn tại");

            using var trans1 = await _humanDb.Database.BeginTransactionAsync();
            using var trans2 = await _payrollDb.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                var humanEmp = new CeoMemo.Models.Human.Employee
                {
                    FullName = dto.FullName,
                    DateOfBirth = dto.DateOfBirth ?? DateOnly.FromDateTime(now),
                    Gender = dto.Gender,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    HireDate = dto.HireDate ?? DateOnly.FromDateTime(now),
                    DepartmentId = dto.DepartmentId,
                    PositionId = dto.PositionId,
                    Status = dto.Status ?? "Active",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _humanDb.Employees.Add(humanEmp);
                await _humanDb.SaveChangesAsync();

                int newEmployeeId = humanEmp.EmployeeId;

                var payrollEmp = new CeoMemo.Models.Payroll.Employee
                {
                    EmployeeId = newEmployeeId,
                    FullName = dto.FullName,
                    DepartmentId = dto.DepartmentId,
                    PositionId = dto.PositionId,
                    Status = dto.Status ?? "Active"
                };
                _payrollDb.Employees.Add(payrollEmp);
                await _payrollDb.SaveChangesAsync();

                await trans1.CommitAsync();
                await trans2.CommitAsync();

                return Ok(new { EmployeeId = newEmployeeId, Message = "Thêm nhân viên thành công" });
            }
            catch (Exception ex)
            {
                await trans1.RollbackAsync();
                await trans2.RollbackAsync();
                Console.WriteLine($"Error adding employee: {ex.Message}");
                return StatusCode(500, "Thêm thất bại. Đã rollback");
            }
        }

        // PUT /update-employee
        [HttpPut("update-employee/{employeeId}")]
        public async Task<IActionResult> UpdateEmployee(int employeeId, [FromBody] EmployeeDto dto)
        {
            if (!IsValidEmployeeDto(dto, out var errorMessage))
                return BadRequest(errorMessage);

            using var trans1 = await _humanDb.Database.BeginTransactionAsync();
            using var trans2 = await _payrollDb.Database.BeginTransactionAsync();

            try
            {
                var emp1 = await _humanDb.Employees.FindAsync(employeeId);
                var emp2 = await _payrollDb.Employees.FindAsync(employeeId);

                if (emp1 == null || emp2 == null)
                    return NotFound("Nhân viên không tồn tại");

                if (!await _humanDb.Departments.AnyAsync(d => d.DepartmentId == dto.DepartmentId))
                    return BadRequest("Phòng ban không tồn tại");
                if (!await _humanDb.Positions.AnyAsync(p => p.PositionId == dto.PositionId))
                    return BadRequest("Chức vụ không tồn tại");

                emp1.FullName = dto.FullName;
                emp1.DateOfBirth = dto.DateOfBirth ?? emp1.DateOfBirth;
                emp1.Gender = dto.Gender;
                emp1.PhoneNumber = dto.PhoneNumber;
                emp1.Email = dto.Email;
                emp1.HireDate = dto.HireDate ?? emp1.HireDate;
                emp1.DepartmentId = dto.DepartmentId;
                emp1.PositionId = dto.PositionId;
                emp1.Status = dto.Status ?? emp1.Status;
                emp1.UpdatedAt = DateTime.UtcNow;

                emp2.FullName = dto.FullName;
                emp2.DepartmentId = dto.DepartmentId;
                emp2.PositionId = dto.PositionId;
                emp2.Status = dto.Status ?? emp2.Status;

                await _humanDb.SaveChangesAsync();
                await _payrollDb.SaveChangesAsync();

                await trans1.CommitAsync();
                await trans2.CommitAsync();

                return Ok("Cập nhật thành công");
            }
            catch (Exception ex)
            {
                await trans1.RollbackAsync();
                await trans2.RollbackAsync();
                Console.WriteLine($"Error updating employee: {ex.Message}");
                return StatusCode(500, "Cập nhật thất bại. Đã rollback");
            }
        }

        // DELETE /delete-employee
        [HttpDelete("delete-employee/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            using var trans1 = await _humanDb.Database.BeginTransactionAsync();
            using var trans2 = await _payrollDb.Database.BeginTransactionAsync();

            try
            {
                var emp1 = await _humanDb.Employees.FindAsync(id);
                var emp2 = await _payrollDb.Employees.FindAsync(id);

                if (emp1 == null || emp2 == null)
                    return NotFound("Không tìm thấy nhân viên");

                bool hasSalary = await _payrollDb.Salaries.AnyAsync(s => s.EmployeeId == id);
                bool hasDividend = await _humanDb.Dividends.AnyAsync(d => d.EmployeeId == id);

                if (hasSalary || hasDividend)
                    return BadRequest("Không thể xóa nhân viên có dữ liệu lương hoặc cổ tức");

                _humanDb.Employees.Remove(emp1);
                _payrollDb.Employees.Remove(emp2);

                await _humanDb.SaveChangesAsync();
                await _payrollDb.SaveChangesAsync();

                await trans1.CommitAsync();
                await trans2.CommitAsync();

                return Ok("Xóa thành công");
            }
            catch (Exception ex)
            {
                await trans1.RollbackAsync();
                await trans2.RollbackAsync();
                Console.WriteLine($"Error deleting employee: {ex.Message}");
                return StatusCode(500, "Xóa thất bại. Đã rollback");
            }
        }

        // GET /reports
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports([FromQuery] int? year, [FromQuery] int? month)
        {
            var query = from emp in _humanDb.Employees
                        join dividend in _humanDb.Dividends on emp.EmployeeId equals dividend.EmployeeId into divs
                        from div in divs.DefaultIfEmpty()
                        join salary in _payrollDb.Salaries on emp.EmployeeId equals salary.EmployeeId into sals
                        from sal in sals.DefaultIfEmpty()
                        select new
                        {
                            emp.EmployeeId,
                            emp.FullName,
                            DepartmentId = emp.DepartmentId ?? 0,
                            PositionId = emp.PositionId ?? 0,
                            DividendAmount = div != null ? div.DividendAmount : 0,
                            NetSalary = sal != null ? sal.NetSalary : 0,
                            SalaryMonth = sal != null ? sal.SalaryMonth : (DateOnly?)null
                        };

            if (year.HasValue)
                query = query.Where(r => r.SalaryMonth == null || r.SalaryMonth.Value.Year == year.Value);
            if (month.HasValue)
                query = query.Where(r => r.SalaryMonth == null || r.SalaryMonth.Value.Month == month.Value);

            var report = await query.ToListAsync();
            return Ok(report);
        }

        // POST /alerts
        [HttpPost("alerts")]
        public IActionResult PostAlert([FromBody] AlertDto dto)
        {
            Console.WriteLine($"ALERT [{dto.Type}] - {dto.Message}");
            return Ok("Đã ghi nhận cảnh báo");
        }

        // GET /employees/search
        [HttpGet("employees/search")]
        public async Task<IActionResult> SearchEmployees([FromQuery] string? id, [FromQuery] string? name, [FromQuery] int? departmentId, [FromQuery] int? positionId)
        {
            var query = _humanDb.Employees
                .Include(e => e.Department)
                .Include(e => e.Position)
                .AsQueryable();

            if (!string.IsNullOrEmpty(id) && int.TryParse(id, out int empId))
                query = query.Where(e => e.EmployeeId == empId);

            if (!string.IsNullOrEmpty(name))
                query = query.Where(e => e.FullName.Contains(name));

            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId);

            if (positionId.HasValue)
                query = query.Where(e => e.PositionId == positionId);

            var result = await query
                .Select(e => new EmployeeDto
                {
                    FullName = e.FullName,
                    DateOfBirth = e.DateOfBirth,
                    Gender = e.Gender,
                    PhoneNumber = e.PhoneNumber,
                    Email = e.Email,
                    HireDate = e.HireDate,
                    DepartmentId = e.DepartmentId ?? 0,
                    PositionId = e.PositionId ?? 0,
                    Status = e.Status,
                    CreatedAt = (DateTime)e.CreatedAt,
                    UpdatedAt = (DateTime)e.UpdatedAt
                })
                .ToListAsync();

            return Ok(result);
        }

        // GET /payroll/by-month
        [HttpGet("payroll/by-month")]
        public async Task<IActionResult> GetPayrollByMonth([FromQuery] string month)
        {
            if (!DateOnly.TryParse($"{month}-01", out var salaryMonth))
                return BadRequest("Tháng không hợp lệ, định dạng đúng là YYYY-MM");

            var salaries = await _payrollDb.Salaries
                .Where(s => s.SalaryMonth.Year == salaryMonth.Year && s.SalaryMonth.Month == salaryMonth.Month)
                .ToListAsync();

            var employees = await _humanDb.Employees.ToListAsync();

            var result = from s in salaries
                         join e in employees on s.EmployeeId equals e.EmployeeId
                         select new SalaryDto
                         {
                             SalaryId = s.SalaryId,
                             SalaryMonth = s.SalaryMonth,
                             BaseSalary = s.BaseSalary,
                             Bonus = (decimal)s.Bonus,
                             Deductions = (decimal)s.Deductions,
                             NetSalary = s.NetSalary,
                             EmployeeID = e.EmployeeId,
                             FullName = e.FullName,
                             DepartmentID = e.DepartmentId ?? 0,
                             PositionID = e.PositionId ?? 0
                         };

            return Ok(result);
        }

        // PUT /update-salary
        [HttpPut("update-salary")]
        public async Task<IActionResult> UpdateSalary([FromBody] SalaryDto dto)
        {
            var salary = await _payrollDb.Salaries.FindAsync(dto.SalaryId);
            if (salary == null)
                return NotFound("Không tìm thấy bản ghi lương");

            salary.BaseSalary = dto.BaseSalary;
            salary.Bonus = dto.Bonus;
            salary.Deductions = dto.Deductions;
            salary.NetSalary = (decimal)(dto.BaseSalary + dto.Bonus - dto.Deductions);

            await _payrollDb.SaveChangesAsync();

            return Ok("Cập nhật lương thành công");
        }

        // GET /salary-history/{employeeId}
        [HttpGet("salary-history/{employeeId}")]
        public async Task<IActionResult> GetSalaryHistory(int employeeId)
        {
            var salaryHistory = await _payrollDb.Salaries
                .Where(s => s.EmployeeId == employeeId)
                .OrderByDescending(s => s.SalaryMonth)
                .ToListAsync();

            return Ok(salaryHistory);
        }

        // GET /salary/chart
        [HttpGet("salary/chart")]
        public async Task<IActionResult> GetSalaryChart([FromQuery] int year)
        {
            var data = await _payrollDb.Salaries
                .Where(s => s.SalaryMonth.Year == year)
                .GroupBy(s => s.SalaryMonth.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalNetSalary = g.Sum(s => s.NetSalary)
                })
                .OrderBy(r => r.Month)
                .ToListAsync();

            return Ok(data);
        }

        // GET /alerts/leave
        [HttpGet("alerts/leave")]
        public async Task<IActionResult> GetLeaveAlerts([FromQuery] int threshold = 5)
        {
            var data = await _payrollDb.Attendances
                .GroupBy(a => a.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    LeaveDays = g.Sum(a => a.LeaveDays)
                })
                .Where(r => r.LeaveDays > threshold)
                .ToListAsync();

            return Ok(data);
        }

        // POST /send-payroll-emails
        [HttpPost("send-payroll-emails")]
        public async Task<IActionResult> SendPayrollEmails()
        {
            var employees = await _humanDb.Employees.ToListAsync();
            var salaries = await _payrollDb.Salaries
                .Where(s => s.SalaryMonth.Month == DateTime.UtcNow.Month &&
                            s.SalaryMonth.Year == DateTime.UtcNow.Year)
                .ToListAsync();

            foreach (var emp in employees)
            {
                var salary = salaries.FirstOrDefault(s => s.EmployeeId == emp.EmployeeId);
                if (salary == null) continue;

                Console.WriteLine($"Gửi email cho {emp.FullName} - Lương tháng: {salary.NetSalary} VND");
            }

            return Ok("Đã gửi email bảng lương");
        }

        // Helper method to validate EmployeeDto
        private bool IsValidEmployeeDto(EmployeeDto dto, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(dto.FullName))
                errorMessage = "Tên không được để trống";
            else if (dto.FullName.Length > 100)
                errorMessage = "Tên không được dài quá 100 ký tự";
            else if (!string.IsNullOrWhiteSpace(dto.Gender) && !new[] { "Male", "Female", "Other" }.Contains(dto.Gender))
                errorMessage = "Giới tính không hợp lệ (Male, Female, Other)";
            else if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && !Regex.IsMatch(dto.PhoneNumber, @"^\+?\d{10,15}$"))
                errorMessage = "Số điện thoại không hợp lệ";
            else if (!string.IsNullOrWhiteSpace(dto.Email) && !new EmailAddressAttribute().IsValid(dto.Email))
                errorMessage = "Email không hợp lệ";
            else if (dto.DateOfBirth.HasValue && dto.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) > DateTime.UtcNow)
                errorMessage = "Ngày sinh không hợp lệ";
            else if (dto.HireDate.HasValue && dto.HireDate.Value.ToDateTime(TimeOnly.MinValue) > DateTime.UtcNow)
                errorMessage = "Ngày tuyển dụng không hợp lệ";
            else if (dto.DepartmentId <= 0)
                errorMessage = "Phòng ban không hợp lệ";
            else if (dto.PositionId <= 0)
                errorMessage = "Chức vụ không hợp lệ";
            else if (!string.IsNullOrWhiteSpace(dto.Status) && !new[] { "Active", "Inactive", "Terminated" }.Contains(dto.Status))
                errorMessage = "Trạng thái không hợp lệ (Active, Inactive, Terminated)";

            return string.IsNullOrEmpty(errorMessage);
        }

        // POST /salary
        [HttpPost("salary")]
        public async Task<IActionResult> AddSalary([FromBody] SalaryDto dto)
        {
            if (!IsValidSalaryDto(dto, out var errorMessage))
                return BadRequest(errorMessage);

            var employeeExists = await _humanDb.Employees.AnyAsync(e => e.EmployeeId == dto.EmployeeID);
            if (!employeeExists)
                return BadRequest("Nhân viên không tồn tại");

            var salaryExists = await _payrollDb.Salaries
                .AnyAsync(s => s.EmployeeId == dto.EmployeeID &&
                              s.SalaryMonth.Year == dto.SalaryMonth.Year &&
                              s.SalaryMonth.Month == dto.SalaryMonth.Month);
            if (salaryExists)
                return BadRequest("Bản ghi lương cho nhân viên này trong tháng đã tồn tại");

            using var transaction = await _payrollDb.Database.BeginTransactionAsync();

            try
            {
                var salary = new CeoMemo.Models.Payroll.Salary
                {
                    EmployeeId = dto.EmployeeID,
                    SalaryMonth = dto.SalaryMonth,
                    BaseSalary = dto.BaseSalary,
                    Bonus = dto.Bonus,
                    Deductions = dto.Deductions,
                    NetSalary = (decimal)(dto.BaseSalary + dto.Bonus - dto.Deductions)
                };

                _payrollDb.Salaries.Add(salary);
                await _payrollDb.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { SalaryId = salary.SalaryId, Message = "Thêm bản ghi lương thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error adding salary: {ex.Message}");
                return StatusCode(500, "Thêm bản ghi lương thất bại. Đã rollback");
            }
        }

        // Helper method to validate SalaryDto
        private bool IsValidSalaryDto(SalaryDto dto, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (dto.EmployeeID <= 0)
                errorMessage = "ID nhân viên không hợp lệ";
            else if (dto.SalaryMonth.ToDateTime(TimeOnly.MinValue) > DateTime.UtcNow)
                errorMessage = "Tháng lương không được trong tương lai";
            else if (dto.BaseSalary < 0)
                errorMessage = "Lương cơ bản không được âm";
            else if (dto.Bonus < 0)
                errorMessage = "Thưởng không được âm";
            else if (dto.Deductions < 0)
                errorMessage = "Khoản khấu trừ không được âm";
            else if (dto.NetSalary != (decimal)(dto.BaseSalary + dto.Bonus - dto.Deductions))
                errorMessage = "Lương ròng không khớp với tính toán";

            return string.IsNullOrEmpty(errorMessage);
        }

        // GET /report-data
        [HttpGet("report-data")]
        public async Task<IActionResult> GetReportData()
        {
            await Task.Delay(100);
            var reportData = new ReportDataDto
            {
                Employees = new List<object> { new { Name = "John Doe", Value = "Details..." } },
                Departments = new List<DepartmentDto>
                {
                    new DepartmentDto { DepartmentId = 1, Name = "IT" },
                    new DepartmentDto { DepartmentId = 2, Name = "HR" }
                }
            };
            return Ok(reportData);
        }

        // GET /notifications
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            await Task.Delay(100);
            var notifications = new List<NotificationDto>
            {
                new NotificationDto { Id = 1, Message = "Anniversary Alert for Employee X", Date = DateTime.Now.AddDays(-1), Status = "Unread", Icon = "fa-birthday-cake", Color = "pink" },
                new NotificationDto { Id = 2, Message = "New Payroll Cycle Starts", Date = DateTime.Now.AddDays(-5), Status = "Read", Icon = "fa-info-circle", Color = "blue" }
            };
            return Ok(notifications);
        }

        [HttpPost("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            await Task.Delay(100);
            return Ok(new { message = $"Notification {id} marked as read." });
        }

        [HttpPost("notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            await Task.Delay(100);
            return Ok(new { message = "All notifications marked as read." });
        }

        [HttpDelete("notifications/{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            await Task.Delay(100);
            return Ok(new { message = $"Notification {id} deleted." });
        }

        // GET /departments
        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                // For debugging - trace the method call
                Console.WriteLine("GetDepartments called");
                
                // First try to fetch from database if available
                List<DepartmentDto> departments = new List<DepartmentDto>();
                
                try
                {
                    // Uncomment if you have _humanDb.Departments available
                    // var dbDepartments = await _humanDb.Departments.ToListAsync();
                    // departments = dbDepartments.Select(d => new DepartmentDto 
                    // { 
                    //     DepartmentId = d.DepartmentId, 
                    //     Name = d.Name, 
                    //     ManagerName = d.ManagerName
                    // }).ToList();
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Database query error: {dbEx.Message}");
                    // Fall back to mock data
                }
                
                // If no departments from database, use mock data
                if (departments.Count == 0)
                {
                    Console.WriteLine("Using mock departments data");
                    departments = new List<DepartmentDto>
                    {
                        new DepartmentDto { DepartmentId = 1, Name = "IT Department", ManagerName = "John Smith" },
                        new DepartmentDto { DepartmentId = 2, Name = "HR Department", ManagerName = "Sarah Johnson" },
                        new DepartmentDto { DepartmentId = 3, Name = "Marketing", ManagerName = "Mike Wilson" }
                    };
                }
                
                return Ok(departments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetDepartments error: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while fetching departments" });
            }
        }

        // Added for CreatedAtRoute in AddDepartment
        [HttpGet("departments/{id}", Name = "GetDepartmentById")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            await Task.Delay(50);
            var mockDepartment = new DepartmentDto { DepartmentId = id, Name = $"Department {id}", ManagerName = $"Manager for Dept {id}" };
            if (id <= 0) return NotFound();
            return Ok(mockDepartment);
        }

        [HttpPost("departments")]
        public async Task<IActionResult> AddDepartment([FromBody] DepartmentDto departmentDto)
        {
            try
            {
                // For debugging - trace the method call and payload
                Console.WriteLine($"AddDepartment called with: {System.Text.Json.JsonSerializer.Serialize(departmentDto)}");
                
                if (!ModelState.IsValid)
                {
                    Console.WriteLine($"ModelState invalid: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                    return BadRequest(ModelState);
                }

                // Additional validation
                if (string.IsNullOrWhiteSpace(departmentDto.Name))
                {
                    ModelState.AddModelError("Name", "Department Name cannot be empty.");
                    Console.WriteLine("Name validation failed");
                }
                
                if (string.IsNullOrWhiteSpace(departmentDto.ManagerName))
                {
                    ModelState.AddModelError("ManagerName", "Department Manager Name cannot be empty.");
                    Console.WriteLine("ManagerName validation failed");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // For a real implementation, you would add to database
                // var newDepartment = new Department { Name = departmentDto.Name, ManagerName = departmentDto.ManagerName };
                // _humanDb.Departments.Add(newDepartment);
                // await _humanDb.SaveChangesAsync();
                // departmentDto.DepartmentId = newDepartment.DepartmentId;
                
                // For now, simulate adding with a new ID
                await Task.Delay(100); // Simulate processing time
                departmentDto.DepartmentId = new Random().Next(100, 1000);
                Console.WriteLine($"Department added with ID: {departmentDto.DepartmentId}");
                
                return CreatedAtRoute("GetDepartmentById", new { id = departmentDto.DepartmentId }, departmentDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddDepartment error: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error while adding department: {ex.Message}" });
            }
        }

        [HttpPut("departments/{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] DepartmentDto departmentDto)
        {
            if (id != departmentDto.DepartmentId)
            {
                return BadRequest("ID mismatch in route and body.");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (string.IsNullOrWhiteSpace(departmentDto.Name) || string.IsNullOrWhiteSpace(departmentDto.ManagerName))
            {
                ModelState.AddModelError("Fields", "Name and Manager Name are required for update.");
                return BadRequest(ModelState);
            }

            await Task.Delay(100);
            return NoContent();
        }

        [HttpDelete("departments/{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            await Task.Delay(100);
            return Ok(new { message = $"Department {id} deleted (stub)." });
        }

        // GET /positions
        [HttpGet("positions")]
        public async Task<IActionResult> GetPositions()
        {
            await Task.Delay(50);
            var positions = new List<PositionDto>
            {
                new PositionDto { PositionId = 1, Name = "Software Developer", DepartmentName = "IT" },
                new PositionDto { PositionId = 2, Name = "HR Manager", DepartmentName = "HR" },
                new PositionDto { PositionId = 3, Name = "Marketing Specialist", DepartmentName = "Marketing" }
            };
            return Ok(positions);
        }

        [HttpPost("positions")]
        public async Task<IActionResult> AddPosition([FromBody] PositionDto positionDto)
        {
            await Task.Delay(100);
            return Ok(new { message = "Position added (stub).", id = new Random().Next(100, 1000) });
        }

        [HttpPut("positions/{id}")]
        public async Task<IActionResult> UpdatePosition(int id, [FromBody] PositionDto positionDto)
        {
            if (id != positionDto.PositionId) return BadRequest("ID mismatch");
            await Task.Delay(100);
            return NoContent();
        }

        [HttpDelete("positions/{id}")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            await Task.Delay(100);
            return Ok(new { message = $"Position {id} deleted (stub)." });
        }
    }
}