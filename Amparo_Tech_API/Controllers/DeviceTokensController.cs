using Amparo_Tech_API.Data;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;
using Amparo_Tech_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Amparo_Tech_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceTokensController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userCtx;
        public DeviceTokensController(AppDbContext ctx, IUserContextService userCtx) { _context = ctx; _userCtx = userCtx; }

        [HttpPost]
        public async Task<ActionResult> Register([FromBody] DeviceTokenDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!_userCtx.TryGetUserId(User, out var myId)) return Unauthorized();

            // infer owner if not provided
            var tipoStr = dto.TipoOwner ?? _userCtx.GetTipoUsuario(User);
            int ownerId = dto.IdOwner ?? myId;
            TipoParticipanteMensagem tipo = TipoParticipanteMensagem.Usuario;
            if (string.Equals(tipoStr, "Instituicao", StringComparison.OrdinalIgnoreCase)) tipo = TipoParticipanteMensagem.Instituicao;
            else if (string.Equals(tipoStr, "Administrador", StringComparison.OrdinalIgnoreCase)) tipo = TipoParticipanteMensagem.Administrador;

            // upsert token
            var existing = await _context.devicetoken.FirstOrDefaultAsync(d => d.Token == dto.Token && d.IdOwner == ownerId && d.TipoOwner == tipo);
            if (existing == null)
            {
                _context.devicetoken.Add(new DeviceToken { Token = dto.Token, IdOwner = ownerId, TipoOwner = tipo, Platform = dto.Platform });
                await _context.SaveChangesAsync();
            }
            else
            {
                existing.Platform = dto.Platform;
                await _context.SaveChangesAsync();
            }

            return Ok(new { Sucesso = true });
        }

        [HttpDelete]
        public async Task<ActionResult> Remove([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest();
            var ent = await _context.devicetoken.FirstOrDefaultAsync(d => d.Token == token);
            if (ent == null) return NotFound();
            _context.devicetoken.Remove(ent);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
