using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Amparo_Tech_API.Services;

namespace Amparo_Tech_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IUserContextService _userCtx;
        public AuthController(IUserContextService userCtx)
        {
            _userCtx = userCtx;
        }

        [HttpGet("verify")] 
        public IActionResult Verify() 
        { 
            var userInfo = new {
                IdUsuario = _userCtx.GetUserId(User),
                Name = _userCtx.GetNome(User),
                Email = _userCtx.GetEmail(User),
                Roles = _userCtx.GetRoles(User).ToArray(), 
                ExpUtc = _userCtx.GetExpUtc(User), 
                NowUtc = DateTime.UtcNow 
            }; 
            return Ok(new { ok = true, userInfo }); 
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(new
            {
                ok = true,
                name = _userCtx.GetNome(User),
                claims,
                expUtc = _userCtx.GetExpUtc(User),
                nowUtc = DateTime.UtcNow
            });
        }

        [AllowAnonymous]
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { ok = true, serverTimeUtc = DateTime.UtcNow });

        [HttpGet("verify-debug")]
        public IActionResult VerifyDebug([FromServices] IConfiguration cfg)
        {
            var auth = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Authorization: Bearer <token> não informado.");

            var token = auth.Substring("Bearer ".Length).Trim();

            var keyStr = cfg["Jwt:Key"];
            if (string.IsNullOrEmpty(keyStr))
                return StatusCode(500, "Jwt:Key não configurado no servidor.");

            var tvp = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidIssuer = cfg["Jwt:Issuer"],
                ValidAudience = cfg["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr))
            };

            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, tvp, out var secToken);
                var claims = principal.Claims.Select(c => new { c.Type, c.Value });
                return Ok(new { ok = true, tokenValid = true, claims });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { ok = false, tokenValid = false, error = ex.Message });
            }
        }
    }
}