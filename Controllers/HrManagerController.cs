using CeoMemo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CeoMemo.Models.Human
using CeoMemo.Models.Payroll;
using CeoMemo.DTOs;
namespace CeoMemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HrManagerController : ControllerBase
    {
        private readonly HumanDbContext _humanDb;
        private readonly PayrollDbContext _payrollDb;

        public HrManagerController(HumanDbContext humanDb, PayrollDbContext payrollDb)
        {
            _humanDb = humanDb;
            _payrollDb = payrollDb;
        }
        // 1. GET /employees
        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _humanDb.Employees.ToListAsync();
            return Ok(employees);
        }

        // 2. GET /payroll
        [HttpGet("payroll")]
        public async Task<IActionResult> GetPayroll()
        {
            var salaries = await _payrollDb.Salaries.ToListAsync();
            var employees = await _humanDb.Employees.ToListAsync();

            var result = from s in salaries
                         join e in employees on s.EmployeeId equals e.EmployeeId
                         select new SalaryDto
                         {
                             SalaryId = s.SalaryId,
                             SalaryMonth = s.SalaryMonth,
                             BaseSalary = s.BaseSalary,
                             Bonus = s.Bonus,
                             Deductions = s.Deductions,
                             NetSalary = s.NetSalary,
                             EmployeeID = e.EmployeeId,
                             FullName = e.FullName,
                             DepartmentID = e.DepartmentId,
                             PositionID = e.PositionId
                         };

            return Ok(result);
        }
        // 3. GET /attendance
        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendance()
        {
            var attendance = await _payrollDb.Attendances.ToListAsync();
            return Ok(attendance);
        }

        // 4. POST /add-employee
        [HttpPost("add-employee")]
        public async Task<IActionResult> AddEmployee([FromBody] EmployeeDto dto)
        {
            // Validate input data
            if (string.IsNullOrEmpty(dto.EmployeeID) || string.IsNullOrEmpty(dto.FullName) ||
                dto.DepartmentID == 0 || dto.PositionID == 0 || dto.BaseSalary <= 0)
            {
                return BadRequest("Thiếu dữ liệu bắt buộc");
            }

            int employeeId = int.Parse(dto.EmployeeID);

            // Check if employee already exists in either table
            bool employeeExists = await _humanDb.Employees.AnyAsync(e => e.EmployeeId == employeeId) ||
                                  await _payrollDb.Employees.AnyAsync(e => e.EmployeeId == employeeId);

            if (employeeExists)
            {
                return Conflict("Mã nhân viên đã tồn tại");
            }

            using var trans1 = await _humanDb.Database.BeginTransactionAsync();
            using var trans2 = await _payrollDb.Database.BeginTransactionAsync();

            try
            {
                // Add to HumanDb
                var humanEmp = new CeoMemo.Models.Human.Employee
                {
                    EmployeeId = employeeId,
                    FullName = dto.FullName,
                    DepartmentId = dto.DepartmentID,
                    PositionId = dto.PositionID,
                    Status = "Active",
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    CreatedAt = DateTime.UtcNow
                };
                _humanDb.Employees.Add(humanEmp);
                await _humanDb.SaveChangesAsync();

                // Add to PayrollDb
                var payrollEmp = new CeoMemo.Models.Payroll.Employee
                {
                    EmployeeId = employeeId,
                    FullName = dto.FullName,
                    DepartmentId = dto.DepartmentID,
                    PositionId = dto.PositionID,
                    Status = "Active"
                };
                _payrollDb.Employees.Add(payrollEmp);
                await _payrollDb.SaveChangesAsync();

                // Commit transactions
                await trans1.CommitAsync();
                await trans2.CommitAsync();

                return Ok("Thêm nhân viên thành công");
            }
            catch
            {
                await trans1.RollbackAsync();
                await trans2.RollbackAsync();
                return StatusCode(500, "Thêm thất bại. Đã rollback");
            }
        }

        // 5. PUT /update-employee
        [HttpPut("update-employee")]
        public async Task<IActionResult> UpdateEmployee([FromBody] EmployeeDto dto)
        {
            int employeeId = int.Parse(dto.EmployeeID);
            var emp1 = await _humanDb.Employees.FindAsync(employeeId);
            var emp2 = await _payrollDb.Employees.FindAsync(employeeId);

            if (emp1 == null || emp2 == null)
                return NotFound("Nhân viên không tồn tại");

            emp1.FullName = dto.FullName;
            emp1.DepartmentId = dto.DepartmentID;
            emp1.PositionId = dto.PositionID;
            emp1.UpdatedAt = DateTime.UtcNow;

            emp2.FullName = dto.FullName;
            emp2.DepartmentId = dto.DepartmentID;
            emp2.PositionId = dto.PositionID;

            await _humanDb.SaveChangesAsync();
            await _payrollDb.SaveChangesAsync();

            return Ok("Cập nhật thành công");
        }

        // 6. DELETE /delete-employee
        [HttpDelete("delete-employee/{id}")]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            int employeeId = int.Parse(id);
            var emp1 = await _humanDb.Employees.FindAsync(employeeId);
            var emp2 = await _payrollDb.Employees.FindAsync(employeeId);

            if (emp1 == null || emp2 == null)
                return NotFound("Không tìm thấy nhân viên");

            bool hasSalary = await _payrollDb.Salaries.AnyAsync(s => s.EmployeeId == employeeId);
            bool hasDividend = await _humanDb.Dividends.AnyAsync(d => d.EmployeeId == employeeId);

            if (hasSalary || hasDividend)
                return BadRequest("Không thể xóa nhân viên có dữ liệu lương hoặc cổ tức");

            _humanDb.Employees.Remove(emp1);
            _payrollDb.Employees.Remove(emp2);

            await _humanDb.SaveChangesAsync();
            await _payrollDb.SaveChangesAsync();

            return Ok("Xóa thành công");
        }
    }
}
