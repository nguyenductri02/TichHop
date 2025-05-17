using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Payroll;

[Table("positions")]
public partial class Position
{
    [Key]
    [Column("PositionID")]
    public int PositionId { get; set; }

    [StringLength(100)]
    public string PositionName { get; set; } = null!;
}
