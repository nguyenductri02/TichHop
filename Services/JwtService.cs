using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CeoMemo.Data;
using CeoMemo.Models.Human;
using Microsoft.IdentityModel.Tokens;

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
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expirationStr = _configuration["Jwt:ExpirationMinutes"] ?? "60";
            var expirationMinutes = int.Parse(expirationStr);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Store the token
            _humanDbContext.Tokens.Add(new Token
            {
                JwtToken = tokenString,
                IsBlacklisted = false,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                CreatedAt = DateTime.UtcNow
            });
            _humanDbContext.SaveChanges();

            return tokenString;
        }

        public void BlacklistToken(string token)
        {
            var existingToken = _humanDbContext.Tokens
                .FirstOrDefault(t => t.JwtToken == token && !t.IsBlacklisted);

            if (existingToken != null)
            {
                existingToken.IsBlacklisted = true;
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
