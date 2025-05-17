using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Payroll;

[Table("employees")]
public partial class Employee
{
    [Key]
    [Column("EmployeeID")]
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = null!;

    [Column("DepartmentID")]
    public int? DepartmentId { get; set; }

    [Column("PositionID")]
    public int? PositionId { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }
}
