﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Payroll;

[Table("departments")]
public partial class Department
{
    [Key]
    [Column("DepartmentID")]
    public int DepartmentId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;
}
