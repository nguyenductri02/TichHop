namespace CeoMemo.Models.Human
{
    public class Token
    {
        public int Id { get; set; }
        public required string JwtToken { get; set; }
        public bool IsBlacklisted { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
