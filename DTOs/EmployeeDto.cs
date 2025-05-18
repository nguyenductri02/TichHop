using System;
using System.ComponentModel.DataAnnotations;

namespace CeoMemo.DTOs
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;
        
        public DateOnly? DateOfBirth { get; set; }
        
        [StringLength(10)]
        public string? Gender { get; set; }
        
        [StringLength(15)]
        public string? PhoneNumber { get; set; }
        
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }
        
        public DateOnly? HireDate { get; set; }
        
        public int DepartmentId { get; set; }
        
        public int PositionId { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
}
