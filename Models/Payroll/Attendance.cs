using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Payroll;

[Table("attendance")]
public partial class Attendance
{
    [Key]
    [Column("AttendanceID")]
    public int AttendanceId { get; set; }

    [Column("EmployeeID")]
    public int? EmployeeId { get; set; }

    public int WorkDays { get; set; }

    public int? AbsentDays { get; set; }

    public int? LeaveDays { get; set; }

    public DateOnly AttendanceMonth { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }
}
