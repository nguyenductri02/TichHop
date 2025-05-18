using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Human;

public partial class Position
{
    [Key]
    [Column("PositionID")]
    public int PositionId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!; // Changed from PositionName to Name

    [Column("DepartmentID")]
    public int DepartmentId { get; set; } // Added DepartmentId property

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Position")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
