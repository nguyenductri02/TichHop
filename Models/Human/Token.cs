using System;
using System.ComponentModel.DataAnnotations;

namespace CeoMemo.Models.Human
{
    public class Token
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string JwtToken { get; set; } = null!;

        public bool IsBlacklisted { get; set; }

        public DateTime IssuedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
