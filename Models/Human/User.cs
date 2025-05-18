using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CeoMemo.Models.Human
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; } = null!;
        
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string Role { get; set; } = null!; // Admin, HR Manager, Payroll Manager, Employee
        
        [StringLength(50)]
        public string Status { get; set; } = "Active";
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
}
