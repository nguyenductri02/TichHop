using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Human;

public partial class Department
{
    [Key]
    [Column("DepartmentID")]
    public int DepartmentId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!; // Changed from DepartmentName to Name

    [StringLength(100)]
    public string? Manager { get; set; } // Added Manager property
    
    [StringLength(255)]
    public string? Description { get; set; } // Added Description property

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Department")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
