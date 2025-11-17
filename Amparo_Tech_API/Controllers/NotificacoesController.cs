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
    public class NotificacoesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userCtx;
        public NotificacoesController(AppDbContext context, IUserContextService userCtx)
        {
            _context = context; _userCtx = userCtx;
        }

        // GET api/notificacoes/minhas
        [HttpGet("minhas")]
        public async Task<ActionResult> Minhas([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!_userCtx.TryGetUserId(User, out var myId)) return Unauthorized();
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);

            // Build query: notifications where destinatario matches current principal by type
            var tipoStr = _userCtx.GetTipoUsuario(User);
            // Map TipoUsuario string to TipoParticipanteMensagem
            TipoParticipanteMensagem tipo;
            if (string.Equals(tipoStr, "Instituicao", StringComparison.OrdinalIgnoreCase)) tipo = TipoParticipanteMensagem.Instituicao;
            else if (string.Equals(tipoStr, "Administrador", StringComparison.OrdinalIgnoreCase)) tipo = TipoParticipanteMensagem.Administrador;
            else tipo = TipoParticipanteMensagem.Usuario;

            var q = _context.notificacao.AsNoTracking().Where(n => n.TipoDestinatario == tipo && n.IdDestinatario == myId);
            var total = await q.CountAsync();
            var items = await q.OrderByDescending(n => n.DataCriacao).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();

            var dto = items.Select(n => new NotificacaoDTO
            {
                IdNotificacao = n.IdNotificacao,
                Titulo = n.Titulo,
                Conteudo = n.Conteudo,
                Link = n.Link,
                IdDestinatario = n.IdDestinatario,
                TipoDestinatario = n.TipoDestinatario.ToString(),
                IsRead = n.IsRead,
                DataCriacao = n.DataCriacao.ToString("o")
            });

            return Ok(new { page, pageSize, total, items = dto });
        }

        // POST api/notificacoes (admin/instituicao can create)
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] NotificacaoDTO dto)
        {
            if (!_userCtx.TryGetUserId(User, out var id)) return Unauthorized();
            // Only admin or institution allowed to push notifications
            if (!_userCtx.IsAdmin(User) && !_userCtx.IsInstituicao(User)) return Forbid();

            TipoParticipanteMensagem tipo = TipoParticipanteMensagem.Usuario;
            if (string.Equals(dto.TipoDestinatario, "Instituicao", StringComparison.OrdinalIgnoreCase)) tipo = TipoParticipanteMensagem.Instituicao;
            else if (string.Equals(dto.TipoDestinatario, "Administrador", StringComparison.OrdinalIgnoreCase)) tipo = TipoParticipanteMensagem.Administrador;

            var n = new Notificacao
            {
                Titulo = dto.Titulo,
                Conteudo = dto.Conteudo,
                Link = dto.Link,
                TipoDestinatario = tipo,
                IdDestinatario = dto.IdDestinatario,
                TipoRemetente = _userCtx.IsAdmin(User) ? TipoParticipanteMensagem.Administrador : TipoParticipanteMensagem.Instituicao,
                IdRemetente = id,
                DataCriacao = DateTime.UtcNow
            };

            _context.notificacao.Add(n);
            await _context.SaveChangesAsync();

            // For now we just return created item; delivering push to MAUI/ painel will be handled by a notification worker / push adapter
            dto.IdNotificacao = n.IdNotificacao;
            return CreatedAtAction(nameof(Minhas), new { id = n.IdNotificacao }, dto);
        }

        // PATCH api/notificacoes/{id}/read
        [HttpPatch("{id:int}/read")]
        public async Task<ActionResult> MarkRead(int id)
        {
            if (!_userCtx.TryGetUserId(User, out var myId)) return Unauthorized();
            var n = await _context.notificacao.FirstOrDefaultAsync(x => x.IdNotificacao == id);
            if (n == null) return NotFound();
            // only recipient can mark as read
            var tipoStr = _userCtx.GetTipoUsuario(User);
            TipoParticipanteMensagem tipo = TipoParticipanteMensagem.Usuario;
            if (string.Equals(tipoStr, "Instituicao", StringComparison.OrdinalIgnoreCase)) tipo = TipoParticipanteMensagem.Instituicao;
            else if (string.Equals(tipoStr, "Administrador", StringComparison.OrdinalIgnoreCase)) tipo = TipoParticipanteMensagem.Administrador;
            if (n.TipoDestinatario != tipo || n.IdDestinatario != myId) return Forbid();

            n.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
