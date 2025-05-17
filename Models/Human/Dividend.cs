using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Models.Human;

public partial class Dividend
{
    [Key]
    [Column("DividendID")]
    public int DividendId { get; set; }

    [Column("EmployeeID")]
    public int? EmployeeId { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal DividendAmount { get; set; }

    public DateOnly DividendDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Dividends")]
    public virtual Employee? Employee { get; set; }
}
