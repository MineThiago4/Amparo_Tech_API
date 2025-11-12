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
    public class MensagensController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMessageCryptoService _crypto;
        private readonly IConfiguration _cfg;
        private readonly IUserContextService _userCtx;
        public MensagensController(AppDbContext context, IMessageCryptoService crypto, IConfiguration cfg, IUserContextService userCtx)
        {
            _context = context; _crypto = crypto; _cfg = cfg; _userCtx = userCtx;
        }

        // GET: api/mensagens?doacaoId=1&page=1&pageSize=50
        // Compat: lista mensagens por doação (sem filtro de par). Conteúdo retorna criptografado como está.
        [HttpGet]
        public async Task<ActionResult> Listar([FromQuery] int doacaoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (doacaoId <= 0) return BadRequest("doacaoId obrigatório.");
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);

            var q = _context.mensagem.AsNoTracking().Where(m => m.IdDoacaoItem == doacaoId).OrderBy(m => m.DataEnvio);
            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var retorno = items.Select(m => new
            {
                m.IdMensagem,
                m.IdDoacaoItem,
                m.IdRemetente,
                m.TipoRemetente,
                m.IdDestinatario,
                m.TipoDestinatario,
                Conteudo = m.Conteudo, // permanece criptografado; cliente faz E2E
                m.DataEnvio
            });

            return Ok(new { page, pageSize, total, items = retorno });
        }

        // GET: api/mensagens/conversa?doacaoId=1&peerType=Usuario&peerId=123&afterId=0&pageSize=100
        // Retorna somente mensagens entre o usuário autenticado e o peer informado (ambas direções), opcionais apenas novas com afterId
        [HttpGet("conversa")]
        public async Task<ActionResult> Conversa([FromQuery] int doacaoId, [FromQuery] TipoParticipanteMensagem peerType, [FromQuery] int peerId, [FromQuery] int? afterId, [FromQuery] int pageSize = 100)
        {
            if (!_userCtx.TryGetUserId(User, out var myId))
                return Unauthorized("Token inválido.");

            if (doacaoId <= 0 || peerId <= 0) return BadRequest("Parâmetros inválidos.");
            pageSize = Math.Clamp(pageSize, 1, 500);

            var q = _context.mensagem.AsNoTracking().Where(m => m.IdDoacaoItem == doacaoId &&
                (
                    (m.IdRemetente == myId && m.TipoRemetente == TipoParticipanteMensagem.Usuario && m.IdDestinatario == peerId && m.TipoDestinatario == peerType)
                    ||
                    (m.IdDestinatario == myId && m.TipoDestinatario == TipoParticipanteMensagem.Usuario && m.IdRemetente == peerId && m.TipoRemetente == peerType)
                ));

            if (afterId.HasValue && afterId.Value > 0)
                q = q.Where(m => m.IdMensagem > afterId.Value);

            var items = await q.OrderBy(m => m.IdMensagem).Take(pageSize).ToListAsync();

            var retorno = items.Select(m => new
            {
                m.IdMensagem,
                m.IdDoacaoItem,
                m.IdRemetente,
                m.TipoRemetente,
                m.IdDestinatario,
                m.TipoDestinatario,
                Conteudo = m.Conteudo, // E2E pelo cliente
                m.DataEnvio
            });

            return Ok(new { items = retorno });
        }

        // GET: api/mensagens/threads?take=50
        // Retorna conversas (do mais recente para o menos recente) agrupadas por (doacaoId, peer)
        [HttpGet("threads")]
        public async Task<ActionResult> Threads([FromQuery] int take = 50)
        {
            if (!_userCtx.TryGetUserId(User, out var myId))
                return Unauthorized("Token inválido.");

            take = Math.Clamp(take, 1, 200);

            var baseQ = _context.mensagem.AsNoTracking()
                .Where(m => (m.IdRemetente == myId && m.TipoRemetente == TipoParticipanteMensagem.Usuario) || (m.IdDestinatario == myId && m.TipoDestinatario == TipoParticipanteMensagem.Usuario));

            var threads = await baseQ
                .Select(m => new
                {
                    m.IdDoacaoItem,
                    PeerType = m.IdRemetente == myId && m.TipoRemetente == TipoParticipanteMensagem.Usuario ? m.TipoDestinatario : m.TipoRemetente,
                    PeerId = m.IdRemetente == myId && m.TipoRemetente == TipoParticipanteMensagem.Usuario ? m.IdDestinatario : m.IdRemetente,
                    m.IdMensagem,
                    m.DataEnvio
                })
                .GroupBy(x => new { x.IdDoacaoItem, x.PeerType, x.PeerId })
                .Select(g => new
                {
                    g.Key.IdDoacaoItem,
                    g.Key.PeerType,
                    g.Key.PeerId,
                    LastId = g.Max(x => x.IdMensagem),
                    LastDate = g.Max(x => x.DataEnvio)
                })
                .OrderByDescending(t => t.LastDate)
                .Take(take)
                .ToListAsync();

            // Enriquecimento básico para instituição
            var instIds = threads.Where(t => t.PeerType == TipoParticipanteMensagem.Instituicao).Select(t => t.PeerId).Distinct().ToList();
            var instMap = await _context.instituicao.AsNoTracking().Where(i => instIds.Contains(i.IdInstituicao)).ToDictionaryAsync(i => i.IdInstituicao, i => i.Nome);

            var result = threads.Select(t => new
            {
                t.IdDoacaoItem,
                t.PeerType,
                t.PeerId,
                PeerNome = t.PeerType == TipoParticipanteMensagem.Instituicao && instMap.TryGetValue(t.PeerId, out var nome) ? nome : null,
                t.LastId,
                t.LastDate
            });

            return Ok(result);
        }

        // POST: api/mensagens
        [HttpPost]
        public async Task<ActionResult> Enviar([FromBody] MensagemEnvioDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!_userCtx.TryGetUserId(User, out var idRemetente))
                return Unauthorized("Token inválido.");

            var doacao = await _context.doacaoitem.AsNoTracking().FirstOrDefaultAsync(d => d.IdDoacaoItem == dto.IdDoacaoItem);
            if (doacao == null) return BadRequest("Doação inválida.");

            if (dto.TipoDestinatario == TipoParticipanteMensagem.Usuario && !_userCtx.IsAdmin(User))
                return Forbid("Envio a usuários permitido apenas para administradores no momento.");

            if (dto.TipoDestinatario == TipoParticipanteMensagem.Instituicao)
            {
                if (!doacao.IdInstituicaoAtribuida.HasValue || doacao.IdInstituicaoAtribuida.Value != dto.IdDestinatario)
                    return BadRequest("Destinatário não corresponde à instituição atribuída.");
                var ok = await _context.instituicao.AnyAsync(i => i.IdInstituicao == dto.IdDestinatario);
                if (!ok) return BadRequest("Instituição não encontrada.");
            }

            var content = dto.Conteudo ?? string.Empty;
            var serverEncrypt = _cfg.GetValue<bool>("Messages:ServerEncrypt");
            if (serverEncrypt)
                content = _crypto.Encrypt(content);

            var msg = new Mensagem
            {
                IdRemetente = idRemetente,
                TipoRemetente = TipoParticipanteMensagem.Usuario,
                IdDestinatario = dto.IdDestinatario,
                TipoDestinatario = dto.TipoDestinatario,
                IdDoacaoItem = dto.IdDoacaoItem,
                Conteudo = content,
                DataEnvio = DateTime.UtcNow
            };

            _context.mensagem.Add(msg);
            await _context.SaveChangesAsync();
            return Ok(new { Sucesso = true, msg.IdMensagem });
        }
    }
}
