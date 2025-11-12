using Amparo_Tech_API.Data;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Amparo_Tech_API.Controllers
{
 [ApiController]
 [Route("api/admin-auth")]
 public class AdminAuthController : ControllerBase
 {
 private readonly AppDbContext _context;
 private readonly IConfiguration _cfg;
 public AdminAuthController(AppDbContext context, IConfiguration cfg) { _context = context; _cfg = cfg; }

 [HttpPost("login")]
 [AllowAnonymous]
 public async Task<ActionResult> Login([FromBody] AdminLoginDTO dto)
 {
 if (!ModelState.IsValid) return BadRequest(ModelState);
 var admin = await _context.administrador.FirstOrDefaultAsync(a => a.Email == dto.Email);
 if (admin == null) return Unauthorized("Administrador não encontrado.");
 if (!BCrypt.Net.BCrypt.Verify(dto.Senha, admin.Senha)) return Unauthorized("Senha incorreta.");
 admin.UltimoLogin = DateTime.UtcNow;
 await _context.SaveChangesAsync();
 var token = GerarToken(admin);
 return Ok(new { token, tipo = "Administrador" });
 }

 private string GerarToken(Administrador admin)
 {
 var keyStr = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado");
 var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr)), SecurityAlgorithms.HmacSha256);
 var claims = new List<Claim>
 {
 new Claim("idAdministrador", admin.IdAdministrador.ToString()),
 new Claim(ClaimTypes.Email, admin.Email),
 new Claim(ClaimTypes.Name, admin.Nome ?? string.Empty),
 new Claim("tipoUsuario", "Administrador")
 };
 var token = new JwtSecurityToken(
 issuer: _cfg["Jwt:Issuer"],
 audience: _cfg["Jwt:Audience"],
 claims: claims,
 expires: DateTime.UtcNow.AddHours(4),
 signingCredentials: creds
 );
 return new JwtSecurityTokenHandler().WriteToken(token);
 }
 }
}
