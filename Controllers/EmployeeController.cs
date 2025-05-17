using CeoMemo.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CeoMemo.Models.Human;
using CeoMemo.Models.Payroll;
using CeoMemo.DTOs;

namespace CeoMemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeeController: ControllerBase
    {

        private readonly HumanDbContext _humanDb;
        private readonly PayrollDbContext _payrollDb;
        public EmployeeController(HumanDbContext humanDb, PayrollDbContext payrollDb)
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
    }
    }

