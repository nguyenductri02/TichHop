namespace CeoMemo.Models.Human
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; } // Store hashed passwords
        public required string Role { get; set; } // Admin, HRManager, PayrollManager, Employee
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
