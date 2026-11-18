using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Back.Data;
using Back.DTOs;
using Back.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Back.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public AuthController(
            AppDbContext db,
            IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpPost("seed-admin")]
        public async Task<IActionResult> SeedAdmin(
            CancellationToken cancellationToken)
        {
            var apiKey =
                _configuration["ApiSettings:ApiKey"];
            var providedKey =
                Request.Headers["X-API-KEY"].ToString();

            if (string.IsNullOrWhiteSpace(apiKey) ||
                providedKey != apiKey)
            {
                return Unauthorized(new
                {
                    message = "Clave de inicialización inválida"
                });
            }

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(
                item => item.UserName == "admin",
                cancellationToken);

            if (usuario != null)
            {
                return Ok(new
                {
                    message = "El usuario admin ya existe"
                });
            }

            usuario = new Usuario
            {
                UserName = "admin",
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword("admin123"),
                Rol = "Admin",
                Activo = true
            };

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message =
                    "Usuario admin creado correctamente"
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login(
            LoginDTO dto,
            CancellationToken cancellationToken)
        {
            var userName = dto.UserName.Trim();
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(
                item => item.UserName == userName && item.Activo,
                cancellationToken);

            if (usuario == null ||
                !BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    usuario.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Usuario o contraseña incorrectos"
                });
            }

            var secretKey =
                _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException(
                    "SecretKey no encontrada.");

            var expiration = DateTime.UtcNow.AddHours(8);
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey));
            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, usuario.UserName),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var token = new JwtSecurityToken(
                issuer: "facturador_net",
                audience: "facturador_net",
                claims: claims,
                expires: expiration,
                signingCredentials: credentials);

            return Ok(new LoginResponseDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpirationUtc = expiration,
                UserName = usuario.UserName,
                Role = usuario.Rol
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordDTO dto,
            CancellationToken cancellationToken)
        {
            var userName = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized();
            }

            var usuario = await _db.Usuarios.FirstOrDefaultAsync(
                item => item.UserName == userName && item.Activo,
                cancellationToken);

            if (usuario == null ||
                !BCrypt.Net.BCrypt.Verify(
                    dto.CurrentPassword,
                    usuario.PasswordHash))
            {
                return BadRequest(new
                {
                    message =
                        "La contraseña actual no es correcta"
                });
            }

            if (dto.NewPassword == dto.CurrentPassword)
            {
                return BadRequest(new
                {
                    message =
                        "La nueva contraseña debe ser distinta"
                });
            }

            usuario.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "Contraseña cambiada correctamente"
            });
        }
    }
}
