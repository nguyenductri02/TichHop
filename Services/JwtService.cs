using CeoMemo.Data;
using CeoMemo.Models.Human;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CeoMemo.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly HumanDbContext _humanDbContext;

        public JwtService(IConfiguration configuration, HumanDbContext humanDbContext)
        {
            _configuration = configuration;
            _humanDbContext = humanDbContext;
        }

        public string GenerateToken(string username, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(30);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Store the token in the database
            var tokenEntity = new Token
            {
                JwtToken = tokenString,
                IsBlacklisted = false,
                IssuedAt = DateTime.Now,
                ExpiresAt = expires,
                CreatedAt = DateTime.Now
            };
            _humanDbContext.Tokens.Add(tokenEntity);
            _humanDbContext.SaveChanges();

            return tokenString;
        }

        public void BlacklistToken(string token)
        {
            var tokenEntity = _humanDbContext.Tokens
                .FirstOrDefault(t => t.JwtToken == token && !t.IsBlacklisted);

            if (tokenEntity != null)
            {
                tokenEntity.IsBlacklisted = true;
                _humanDbContext.SaveChanges();
            }
        }

        public bool IsTokenBlacklisted(string token)
        {
            return _humanDbContext.Tokens
                .Any(t => t.JwtToken == token && t.IsBlacklisted);
        }
    }
}
