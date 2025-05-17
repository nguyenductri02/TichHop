using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Payroll;

[Table("salaries")]
public partial class Salary
{
    [Key]
    [Column("SalaryID")]
    public int SalaryId { get; set; }

    [Column("EmployeeID")]
    public int? EmployeeId { get; set; }

    public DateOnly SalaryMonth { get; set; }

    [Precision(12, 2)]
    public decimal BaseSalary { get; set; }

    [Precision(12, 2)]
    public decimal? Bonus { get; set; }

    [Precision(12, 2)]
    public decimal? Deductions { get; set; }

    [Precision(12, 2)]
    public decimal NetSalary { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }
}
