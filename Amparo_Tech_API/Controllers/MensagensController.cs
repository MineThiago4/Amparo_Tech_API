using Amparo_Tech_API.Data;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;
using Amparo_Tech_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Amparo_Tech_API.Extensions;

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
        private readonly INotificationService _notify;
        public MensagensController(AppDbContext context, IMessageCryptoService crypto, IConfiguration cfg, IUserContextService userCtx, INotificationService notify)
        {
            _context = context; _crypto = crypto; _cfg = cfg; _userCtx = userCtx; _notify = notify;
        }

        // GET: api/mensagens?doacaoId=1&page=1&pageSize=50
        // Backwards compatible: if doacaoId provided, list messages for that donation.
        // New: if peerType & peerId provided, list messages between current user and that peer across donations.
        [HttpGet]
        public async Task<ActionResult> Listar([FromQuery] int? doacaoId, [FromQuery] TipoParticipanteMensagem? peerType, [FromQuery] int? peerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (!_userCtx.TryGetUserId(User, out var myId))
                return Unauthorized("Token inválido.");

            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);

            IQueryable<Mensagem> q = _context.mensagem.AsNoTracking().OrderBy(m => m.DataEnvio);

            if (doacaoId.HasValue && doacaoId.Value > 0)
            {
                q = q.Where(m => m.IdDoacaoItem == doacaoId.Value);
            }
            else if (peerType.HasValue && peerId.HasValue)
            {
                // Messages where current user and peer are participants in either direction
                var pt = peerType.Value;
                var pid = peerId.Value;
                q = q.Where(m =>
                    (m.IdRemetente == myId && m.IdDestinatario == pid && m.TipoDestinatario == pt) ||
                    (m.IdDestinatario == myId && m.IdRemetente == pid && m.TipoRemetente == pt)
                );
            }
            else
            {
                return BadRequest("Informe doacaoId ou (peerType e peerId).");
            }

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var retorno = items.Select(m => new MensagemViewDTO
            {
                IdMensagem = m.IdMensagem,
                IdDoacaoItem = m.IdDoacaoItem,
                IdRemetente = m.IdRemetente,
                TipoRemetente = m.TipoRemetente.ToString(),
                IdDestinatario = m.IdDestinatario,
                TipoDestinatario = m.TipoDestinatario.ToString(),
                Conteudo = m.Conteudo,
                DataEnvio = m.DataEnvio
            });

            return Ok(new { page, pageSize, total, items = retorno });
        }

        // GET: api/mensagens/conversa?peerType=Usuario&peerId=123&afterId=0&pageSize=100[&doacaoId=14]
        // Returns messages between authenticated principal and peer across all donations or filtered by donation when doacaoId provided.
        [HttpGet("conversa")]
        public async Task<ActionResult> Conversa([FromQuery] TipoParticipanteMensagem peerType, [FromQuery] int peerId, [FromQuery] int? afterId, [FromQuery] int? doacaoId, [FromQuery] int pageSize = 100)
        {
            if (!_userCtx.TryGetUserId(User, out var myId))
                return Unauthorized("Token inválido.");

            if (peerId <= 0) return BadRequest("peerId inválido.");
            pageSize = Math.Clamp(pageSize, 1, 500);

            var q = _context.mensagem.AsNoTracking().Where(m =>
                (
                    (m.IdRemetente == myId && m.IdDestinatario == peerId && m.TipoDestinatario == peerType)
                    ||
                    (m.IdDestinatario == myId && m.IdRemetente == peerId && m.TipoRemetente == peerType)
                )
            );

            if (doacaoId.HasValue && doacaoId.Value > 0)
                q = q.Where(m => m.IdDoacaoItem == doacaoId.Value);

            if (afterId.HasValue && afterId.Value > 0)
                q = q.Where(m => m.IdMensagem > afterId.Value);

            var items = await q.OrderBy(m => m.IdMensagem).Take(pageSize).ToListAsync();

            var retorno = items.Select(m => new MensagemViewDTO
            {
                IdMensagem = m.IdMensagem,
                IdDoacaoItem = m.IdDoacaoItem,
                IdRemetente = m.IdRemetente,
                TipoRemetente = m.TipoRemetente.ToString(),
                IdDestinatario = m.IdDestinatario,
                TipoDestinatario = m.TipoDestinatario.ToString(),
                Conteudo = m.Conteudo,
                DataEnvio = m.DataEnvio
            });

            object? peerInfo = null;
            if (peerType == TipoParticipanteMensagem.Instituicao)
            {
                var inst = await _context.instituicao.AsNoTracking().Include(i => i.Endereco).FirstOrDefaultAsync(i => i.IdInstituicao == peerId);
                if (inst != null)
                {
                    peerInfo = _userCtx.IsAdmin(User) ? inst.ToAdminDTO() : inst.ToViewDTO();
                }
            }

            return Ok(new { peer = peerInfo, items = retorno });
        }

        // GET: api/mensagens/threads?take=50
        // Returns threads grouped by peer (peerType + peerId) across all donations
        [HttpGet("threads")]
        public async Task<ActionResult> Threads([FromQuery] int take = 50)
        {
            if (!_userCtx.TryGetUserId(User, out var myId))
                return Unauthorized("Token inválido.");

            take = Math.Clamp(take, 1, 200);

            var baseQ = _context.mensagem.AsNoTracking()
                .Where(m => (m.IdRemetente == myId) || (m.IdDestinatario == myId));

            var threads = await baseQ
                .Select(m => new
                {
                    PeerType = m.IdRemetente == myId ? m.TipoDestinatario : m.TipoRemetente,
                    PeerId = m.IdRemetente == myId ? m.IdDestinatario : m.IdRemetente,
                    m.IdMensagem,
                    m.IdDoacaoItem,
                    m.DataEnvio,
                    m.Conteudo
                })
                .GroupBy(x => new { x.PeerType, x.PeerId })
                .Select(g => new
                {
                    g.Key.PeerType,
                    g.Key.PeerId,
                    LastId = g.Max(x => x.IdMensagem),
                    LastDate = g.Max(x => x.DataEnvio),
                    LastDoacaoId = g.OrderByDescending(x => x.DataEnvio).FirstOrDefault().IdDoacaoItem,
                    LastConteudo = g.OrderByDescending(x => x.DataEnvio).FirstOrDefault().Conteudo
                })
                .OrderByDescending(t => t.LastDate)
                .Take(take)
                .ToListAsync();

            // Enriquecimento para instituições
            var instIds = threads.Where(t => t.PeerType == TipoParticipanteMensagem.Instituicao).Select(t => t.PeerId).Distinct().ToList();
            var insts = await _context.instituicao.AsNoTracking().Where(i => instIds.Contains(i.IdInstituicao)).Include(i => i.Endereco).ToListAsync();
            var isAdmin = _userCtx.IsAdmin(User);
            var instMap = insts.ToDictionary(i => i.IdInstituicao, i => isAdmin ? (object)i.ToAdminDTO() : (object)i.ToViewDTO());

            var result = threads.Select(t => new
            {
                t.PeerType,
                t.PeerId,
                PeerNome = t.PeerType == TipoParticipanteMensagem.Instituicao && instMap.TryGetValue(t.PeerId, out var nomeObj) ? ((nomeObj is Amparo_Tech_API.DTOs.InstituicaoViewDTO v) ? v.Nome : ((Amparo_Tech_API.DTOs.InstituicaoAdminDTO)nomeObj).Nome) : null,
                PeerInstituicao = t.PeerType == TipoParticipanteMensagem.Instituicao && instMap.TryGetValue(t.PeerId, out var dto) ? dto : null,
                t.LastId,
                t.LastDate,
                t.LastDoacaoId,
                LastConteudo = t.LastConteudo
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

            // IdDoacaoItem is now optional; if provided validate it
            if (dto.IdDoacaoItem.HasValue)
            {
                var doacao = await _context.doacaoitem.AsNoTracking().FirstOrDefaultAsync(d => d.IdDoacaoItem == dto.IdDoacaoItem.Value);
                if (doacao == null) return BadRequest("Doação inválida.");

                if (dto.TipoDestinatario == TipoParticipanteMensagem.Instituicao)
                {
                    if (!doacao.IdInstituicaoAtribuida.HasValue || doacao.IdInstituicaoAtribuida.Value != dto.IdDestinatario)
                        return BadRequest("Destinatário não corresponde à instituição atribuída.");
                    var ok = await _context.instituicao.AnyAsync(i => i.IdInstituicao == dto.IdDestinatario);
                    if (!ok) return BadRequest("Instituição não encontrada.");
                }
            }
            else
            {
                // If no donation specified, allow messages between participants but validate existence for institutions
                if (dto.TipoDestinatario == TipoParticipanteMensagem.Instituicao)
                {
                    var ok = await _context.instituicao.AnyAsync(i => i.IdInstituicao == dto.IdDestinatario);
                    if (!ok) return BadRequest("Instituição não encontrada.");
                }
            }

            if (dto.TipoDestinatario == TipoParticipanteMensagem.Usuario && !_userCtx.IsAdmin(User))
                return Forbid("Envio a usuários permitido apenas para administradores no momento.");

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

            // create notification for recipient
            var notif = new NotificacaoDTO
            {
                Titulo = "Nova mensagem",
                Conteudo = dto.Conteudo ?? string.Empty,
                Link = null,
                IdDestinatario = dto.IdDestinatario,
                TipoDestinatario = dto.TipoDestinatario.ToString(),
                IsRead = false,
                DataCriacao = DateTime.UtcNow.ToString("o")
            };
            await _notify.SendNotificationAsync(notif);

            return Ok(new { Sucesso = true, msg.IdMensagem });
        }
    }
}
