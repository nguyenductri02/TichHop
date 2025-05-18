using System;
using System.ComponentModel.DataAnnotations;

namespace CeoMemo.DTOs
{
    public class AlertDto
    {
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = null!;
        
        [Required]
        [StringLength(255)]
        public string Message { get; set; } = null!;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(100)]
        public string? Source { get; set; }
        
        public int? RelatedId { get; set; }
        
        public string? Severity { get; set; } = "Normal";
    }
}
