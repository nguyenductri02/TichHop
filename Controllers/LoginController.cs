using CeoMemo.Data;
using CeoMemo.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace CeoMemo.Controllers
{
    public class LoginController : ControllerBase
    {
        [Route("api/[controller]")]
        [ApiController]
        public class AuthController : ControllerBase
        {
            private readonly JwtService _jwtService;
            private readonly HumanDbContext _humanDbContext;

            public AuthController(JwtService jwtService, HumanDbContext humanDbContext)
            {
                _jwtService = jwtService;
                _humanDbContext = humanDbContext;
            }

            [HttpPost("login")]
            public IActionResult Login([FromBody] LoginRequest request)
            {
                var user = _humanDbContext.Users
                    .FirstOrDefault(u => u.Username == request.Email); // Updated to use 'Email' instead of 'Username'

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized("Invalid credentials.");
                }

                var token = _jwtService.GenerateToken(user.Username, user.Role);
                return Ok(new { Token = token });
            }

            [HttpPost("logout")]
            public IActionResult Logout()
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest("Token is required.");
                }

                _jwtService.BlacklistToken(token);
                return Ok("Logged out successfully.");
            }
        }
    }
}
