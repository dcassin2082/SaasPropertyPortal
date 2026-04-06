using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PropertyPortal.Application.Common.Interfaces;
using PropertyPortal.Application.DTOs.Auth;
using PropertyPortal.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PropertyPortal.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;

        public AuthController(IUnitOfWork uow, IConfiguration config)
        {
            _uow = uow;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModelDto login)
        {
            // 1. Find the user (Query bypasses Tenant Filter for login)
            var user = await _uow.Users.Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == login.Email);

            // 2. Verify credentials using BCrypt
            // BCrypt.Verify compares the plain text 'login.Password' against the 'user.PasswordHash'
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            // 3. Generate the Token
            var token = GenerateJwtToken(user);

            return Ok(new { token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterTenantDto dto)
        {
            // 1. Create the Tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = dto.CompanyName,
                CreatedBy = Guid.Empty // System-created
            };

            // 2. Create the Admin User for that Tenant
            var adminUser = new User
            {
                Tenant = tenant,
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = dto.AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), // Use Hashing!
                Role = "Admin",
                CreatedBy = Guid.Empty
            };

            // 3. Save both in ONE transaction
            await _uow.Tenants.PostAsync(tenant);
            await _uow.Users.PostAsync(adminUser);

            // CompleteAsync ensures both succeed or both fail (Atomicity)
            await _uow.CompleteAsync();

        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // UserId for GetUserId()
            new Claim("TenantId", user.TenantId.ToString()),           // TenantId for Middleware
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
