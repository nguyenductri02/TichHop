using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Human;

[Index("Email", Name = "UQ__Employee__A9D105341DBCD817", IsUnique = true)]
public partial class Employee
{
    [Key]
    [Column("EmployeeID")]
    public int EmployeeId { get; set; }

    [StringLength(100)]
    public string FullName { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public DateOnly HireDate { get; set; }

    [Column("DepartmentID")]
    public int? DepartmentId { get; set; }
    [Column("PositionID")]
    public int? PositionId { get; set; }
    public string? Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("DepartmentId")]
    [InverseProperty("Employees")]
    public virtual Department? Department { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<Dividend> Dividends { get; set; } = new List<Dividend>();

    [ForeignKey("PositionId")]
    [InverseProperty("Employees")]
    public virtual Position? Position { get; set; }
}
