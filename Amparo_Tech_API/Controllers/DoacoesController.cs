using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Amparo_Tech_API.Data;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;
using Amparo_Tech_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Amparo_Tech_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DoacoesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMediaPolicyService _media;
        private readonly IUserContextService _userCtx;
        public DoacoesController(AppDbContext context, IMediaPolicyService media, IUserContextService userCtx)
        {
            _context = context; _media = media; _userCtx = userCtx;
        }

        private static string ComputeETag(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw ?? string.Empty));
            return "W/\"" + Convert.ToBase64String(bytes) + "\"";
        }

        // GET: api/doacoes
        [HttpGet]
        public async Task<ActionResult> Get(
            [FromQuery] int? categoria,
            [FromQuery] DoacaoStatusEnum? status,
            [FromQuery] int? instituicaoId,
            [FromQuery] int? doadorId,
            [FromQuery] DateTime? dataIni,
            [FromQuery] DateTime? dataFim,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "data",
            [FromQuery] string? sortDir = "desc")
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _context.doacaoitem.AsNoTracking().AsQueryable();
            if (categoria.HasValue) q = q.Where(d => d.IdCategoria == categoria.Value);
            if (status.HasValue) q = q.Where(d => d.Status == status.Value);
            if (instituicaoId.HasValue) q = q.Where(d => d.IdInstituicaoAtribuida == instituicaoId.Value);
            if (doadorId.HasValue) q = q.Where(d => d.IdDoador == doadorId.Value);
            if (dataIni.HasValue) q = q.Where(d => d.DataDoacao >= dataIni.Value);
            if (dataFim.HasValue) q = q.Where(d => d.DataDoacao <= dataFim.Value);

            // Ordenação
            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            q = (sortBy?.ToLowerInvariant()) switch
            {
                "titulo" => (desc ? q.OrderByDescending(d => d.Titulo) : q.OrderBy(d => d.Titulo)),
                "status" => (desc ? q.OrderByDescending(d => d.Status) : q.OrderBy(d => d.Status)),
                "categoria" => (desc ? q.OrderByDescending(d => d.IdCategoria) : q.OrderBy(d => d.IdCategoria)),
                _ => (desc ? q.OrderByDescending(d => d.DataDoacao) : q.OrderBy(d => d.DataDoacao))
            };

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(d => new
                {
                    d.IdDoacaoItem,
                    d.Titulo,
                    d.Descricao,
                    d.Condicao,
                    d.IdCategoria,
                    d.IdInstituicaoAtribuida,
                    d.Status,
                    d.DataDoacao,
                    d.MidiaId
                })
                .ToListAsync();

            // ETag para coleção baseada nos parâmetros + ids/versões dos itens
            var signature = $"list:{page}:{pageSize}:{categoria}:{status}:{instituicaoId}:{doadorId}:{dataIni}:{dataFim}:{sortBy}:{sortDir}:{total}:{string.Join(',', items.Select(i => $"{i.IdDoacaoItem}-{i.Status}-{i.DataDoacao.Ticks}"))}";
            var etag = ComputeETag(signature);
            var inm = Request.Headers["If-None-Match"].ToString();
            if (!string.IsNullOrEmpty(inm) && string.Equals(inm, etag, StringComparison.Ordinal))
                return StatusCode(304);

            Response.Headers["ETag"] = etag;
            return Ok(new { page, pageSize, total, items });
        }

        // GET: api/doacoes/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById(int id)
        {
            var d = await _context.doacaoitem.AsNoTracking().Include(x => x.InstituicaoAtribuida).FirstOrDefaultAsync(x => x.IdDoacaoItem == id);
            if (d == null) return NotFound();
            var midias = await _context.doacaomidia.Where(m => m.IdDoacaoItem == id).OrderBy(m => m.Ordem).ToListAsync();

            // ETag por item
            var sig = $"item:{d.IdDoacaoItem}:{d.Status}:{d.DataDoacao.Ticks}:{d.MidiaId}:{d.IdInstituicaoAtribuida}:{midias.Count}:{string.Join(',', midias.Select(m => m.MidiaId))}";
            var etag = ComputeETag(sig);
            var inm = Request.Headers["If-None-Match"].ToString();
            if (!string.IsNullOrEmpty(inm) && string.Equals(inm, etag, StringComparison.Ordinal))
                return StatusCode(304);

            Response.Headers["ETag"] = etag;
            return Ok(new { doacao = d, midias });
        }

        // GET: api/doacoes/minhas (para Doador)
        [HttpGet("minhas")]
        public async Task<ActionResult> Minhas([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!_userCtx.TryGetUserId(User, out var idUsuario))
                return Unauthorized("Token inválido.");

            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _context.doacaoitem.AsNoTracking().Where(d => d.IdDoador == idUsuario);
            var total = await q.CountAsync();
            var items = await q.OrderByDescending(d => d.DataDoacao)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(d => new { d.IdDoacaoItem, d.Titulo, d.Status, d.DataDoacao })
                .ToListAsync();
            return Ok(new { page, pageSize, total, items });
        }

        // GET: api/doacoes/minhas-solicitacoes (para Beneficiário)
        [HttpGet("minhas-solicitacoes")]
        public async Task<ActionResult> MinhasSolicitacoes([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!_userCtx.TryGetUserId(User, out var idUsuario))
                return Unauthorized("Token inválido.");

            if (!_userCtx.IsBeneficiario(User))
                return Forbid("Somente beneficiários.");

            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);

            var doacaoIdsQ = _context.mensagem.AsNoTracking()
                .Where(m => m.IdRemetente == idUsuario && m.TipoRemetente == TipoParticipanteMensagem.Usuario && m.TipoDestinatario == TipoParticipanteMensagem.Instituicao)
                .Select(m => m.IdDoacaoItem)
                .Distinct();

            var q = _context.doacaoitem.AsNoTracking().Where(d => doacaoIdsQ.Contains(d.IdDoacaoItem));
            var total = await q.CountAsync();
            var items = await q.OrderByDescending(d => d.DataDoacao)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(d => new { d.IdDoacaoItem, d.Titulo, d.Status, d.DataDoacao })
                .ToListAsync();
            return Ok(new { page, pageSize, total, items });
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] DoacaoCadastroDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!_userCtx.TryGetUserId(User, out var idDoador))
                return Unauthorized("Token inválido.");

            if (!_userCtx.IsDoador(User) && !_userCtx.IsAdmin(User))
                return Forbid("Apenas usuários Doadores ou Administradores podem cadastrar doações.");

            var categoriaExiste = await _context.categoria.AnyAsync(c => c.IdCategoria == dto.IdCategoria);
            if (!categoriaExiste) return BadRequest("Categoria inválida.");

            if (dto.IdInstituicaoAtribuida.HasValue)
            {
                var instOk = await _context.instituicao.AnyAsync(i => i.IdInstituicao == dto.IdInstituicaoAtribuida.Value);
                if (!instOk) return BadRequest("Instituição atribuída inválida.");
            }

            var midiaIds = (dto.MidiaIds ?? new List<string>())
                .Concat(string.IsNullOrWhiteSpace(dto.MidiaId) ? Enumerable.Empty<string>() : new[] { dto.MidiaId })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            try { _media.ValidateIdsOrThrow(midiaIds, out _, out _); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }

            var entity = new DoacaoItem
            {
                IdDoador = idDoador,
                IdCategoria = dto.IdCategoria,
                Titulo = dto.Titulo,
                Descricao = dto.Descricao,
                Condicao = dto.Condicao,
                IdInstituicaoAtribuida = dto.IdInstituicaoAtribuida,
                MidiaId = midiaIds.FirstOrDefault(),
                Status = DoacaoStatusEnum.Disponivel,
                DataDoacao = DateTime.UtcNow
            };

            _context.doacaoitem.Add(entity);
            await _context.SaveChangesAsync();

            if (midiaIds.Count > 0)
            {
                int ordem = 1;
                foreach (var id in midiaIds)
                {
                    var (_, ext) = _media.ResolveFileById(id);
                    var tipo = _media.IsVideoExt(ext) ? "Video" : "Imagem";
                    _context.doacaomidia.Add(new DoacaoMidia
                    {
                        IdDoacaoItem = entity.IdDoacaoItem,
                        MidiaId = id,
                        Tipo = tipo,
                        Ordem = ordem++
                    });
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(Post), new { id = entity.IdDoacaoItem }, new { Sucesso = true, Mensagem = "Doação cadastrada com sucesso!" });
        }

        // PUT: api/doacoes/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, [FromBody] DoacaoAtualizacaoDTO dto)
        {
            if (!_userCtx.TryGetUserId(User, out var idUsuario))
                return Unauthorized("Token inválido.");

            var doacao = await _context.doacaoitem.FirstOrDefaultAsync(d => d.IdDoacaoItem == id);
            if (doacao == null) return NotFound();

            // Permissão: dono (doador) ou administrador ou instituição atribuída
            bool isDono = doacao.IdDoador == idUsuario && _userCtx.IsDoador(User);
            bool isAdmin = _userCtx.IsAdmin(User);
            bool isInstituicaoAtribuida = _userCtx.IsInstituicao(User) && _userCtx.GetInstituicaoId(User) == doacao.IdInstituicaoAtribuida;
            if (!isDono && !isAdmin && !isInstituicaoAtribuida)
                return Forbid("Sem permissão para editar esta doação.");

            if (!isAdmin && !isInstituicaoAtribuida && doacao.Status != DoacaoStatusEnum.Disponivel)
                return BadRequest("Só é possível editar doações disponíveis.");

            if (!string.IsNullOrWhiteSpace(dto.Titulo)) doacao.Titulo = dto.Titulo;
            if (!string.IsNullOrWhiteSpace(dto.Descricao)) doacao.Descricao = dto.Descricao;
            if (!string.IsNullOrWhiteSpace(dto.Condicao)) doacao.Condicao = dto.Condicao;
            if (dto.IdCategoria.HasValue)
            {
                var categoriaExiste = await _context.categoria.AnyAsync(c => c.IdCategoria == dto.IdCategoria.Value);
                if (!categoriaExiste) return BadRequest("Categoria inválida.");
                doacao.IdCategoria = dto.IdCategoria.Value;
            }
            if (dto.IdInstituicaoAtribuida.HasValue && (isAdmin || isDono)) // só admin ou doador original podem reatribuir
            {
                var instOk = await _context.instituicao.AnyAsync(i => i.IdInstituicao == dto.IdInstituicaoAtribuida.Value);
                if (!instOk) return BadRequest("Instituição atribuída inválida.");
                doacao.IdInstituicaoAtribuida = dto.IdInstituicaoAtribuida.Value;
            }

            if (dto.SubstituirMidias == true && (isAdmin || isDono)) // instituição não substitui midias do item
            {
                var novos = (dto.MidiaIds ?? new List<string>())
                    .Concat(string.IsNullOrWhiteSpace(dto.MidiaId) ? Enumerable.Empty<string>() : new[] { dto.MidiaId })
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                try { _media.ValidateIdsOrThrow(novos, out _, out _); }
                catch (InvalidOperationException ex) { return BadRequest(ex.Message); }

                var atuais = await _context.doacaomidia.Where(m => m.IdDoacaoItem == doacao.IdDoacaoItem).ToListAsync();
                var idsAtuais = atuais.Select(a => a.MidiaId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                if (atuais.Count > 0) _context.doacaomidia.RemoveRange(atuais);
                if (!string.IsNullOrWhiteSpace(doacao.MidiaId)) idsAtuais.Add(doacao.MidiaId);
                idsAtuais = idsAtuais.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var mid in idsAtuais)
                {
                    var (path, _) = _media.ResolveFileById(mid);
                    if (path != null && System.IO.File.Exists(path))
                    {
                        try { System.IO.File.Delete(path); } catch { }
                    }
                }

                if (novos.Count > 0)
                {
                    int ordem = 1;
                    foreach (var idm in novos)
                    {
                        var (_, ext) = _media.ResolveFileById(idm);
                        var tipo = _media.IsVideoExt(ext) ? "Video" : "Imagem";
                        _context.doacaomidia.Add(new DoacaoMidia
                        {
                            IdDoacaoItem = doacao.IdDoacaoItem,
                            MidiaId = idm,
                            Tipo = tipo,
                            Ordem = ordem++
                        });
                    }
                    doacao.MidiaId = novos.FirstOrDefault();
                }
                else
                {
                    doacao.MidiaId = null;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { Sucesso = true });
        }

        // DELETE: api/doacoes/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (!_userCtx.TryGetUserId(User, out var idUsuario))
                return Unauthorized("Token inválido.");

            var doacao = await _context.doacaoitem.FirstOrDefaultAsync(d => d.IdDoacaoItem == id);
            if (doacao == null) return NotFound();
            bool isDono = doacao.IdDoador == idUsuario && _userCtx.IsDoador(User);
            bool isAdmin = _userCtx.IsAdmin(User);
            if (!isDono && !isAdmin) return Forbid("Apenas dono ou administrador pode excluir.");
            if (!isAdmin && doacao.Status != DoacaoStatusEnum.Disponivel) return BadRequest("Só é possível excluir doações disponíveis.");

            _context.doacaoitem.Remove(doacao);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/doacoes/{id}/solicitar
        [HttpPost("{id:int}/solicitar")]
        public async Task<ActionResult> Solicitar(int id, [FromBody] SolicitacaoDoacaoDTO dto)
        {
            if (!_userCtx.TryGetUserId(User, out var idUsuario))
                return Unauthorized("Token inválido.");

            if (!_userCtx.IsBeneficiario(User))
                return Forbid("Apenas Beneficiários podem solicitar doações.");

            var doacao = await _context.doacaoitem.FirstOrDefaultAsync(d => d.IdDoacaoItem == id);
            if (doacao == null) return NotFound("Doação não encontrada.");
            if (doacao.Status != DoacaoStatusEnum.Disponivel) return BadRequest("Doação não está disponível.");
            if (doacao.IdDoador == idUsuario) return BadRequest("Você não pode solicitar sua própria doação.");

            if (!(doacao.IdInstituicaoAtribuida.HasValue))
                return BadRequest("Doação não está atribuída a nenhuma instituição.");

            var instituicao = await _context.instituicao.AsNoTracking().FirstOrDefaultAsync(i => i.IdInstituicao == doacao.IdInstituicaoAtribuida!.Value);
            if (instituicao == null)
                return BadRequest("Instituição atribuída não encontrada.");

            var conteudoMsg = $"Olá! eu gostaria de requisitar esta doação! (ID: {doacao.IdDoacaoItem})";

            var msg = new Mensagem
            {
                IdRemetente = idUsuario,
                TipoRemetente = TipoParticipanteMensagem.Usuario,
                IdDestinatario = instituicao.IdInstituicao,
                TipoDestinatario = TipoParticipanteMensagem.Instituicao,
                IdDoacaoItem = doacao.IdDoacaoItem,
                Conteudo = conteudoMsg,
                DataEnvio = DateTime.UtcNow
            };
            _context.mensagem.Add(msg);

            // Fluxo novo: Aguardando até confirmação do admin/instituição
            doacao.Status = DoacaoStatusEnum.Aguardando;
            doacao.RequeridoPor = idUsuario;

            await _context.SaveChangesAsync();
            return Ok(new { Sucesso = true, Mensagem = "Solicitação enviada à instituição responsável." });
        }

        // NOVO: POST api/doacoes/{id}/cancelar-solicitacao (Beneficiário)
        [HttpPost("{id:int}/cancelar-solicitacao")]
        public async Task<ActionResult> CancelarSolicitacao(int id)
        {
            if (!_userCtx.TryGetUserId(User, out var idUsuario))
                return Unauthorized("Token inválido.");

            if (!_userCtx.IsBeneficiario(User))
                return Forbid("Apenas Beneficiários podem cancelar solicitações.");

            var doacao = await _context.doacaoitem.FirstOrDefaultAsync(d => d.IdDoacaoItem == id);
            if (doacao == null) return NotFound("Doação não encontrada.");

            if (doacao.RequeridoPor != idUsuario)
                return Forbid("Você só pode cancelar sua própria solicitação.");

            if (doacao.Status != DoacaoStatusEnum.Aguardando && doacao.Status != DoacaoStatusEnum.Solicitado)
                return BadRequest("A doação não está em estado de solicitação.");

            doacao.RequeridoPor = null;
            doacao.Status = DoacaoStatusEnum.Disponivel;
            await _context.SaveChangesAsync();

            return Ok(new { Sucesso = true, Mensagem = "Solicitação cancelada e doação voltou a disponível." });
        }

        // PATCH: api/doacoes/{id}/confirmar?aceita=true|false
        [HttpPatch("{id:int}/confirmar")]
        public async Task<ActionResult> ConfirmarSolicitacao(int id, [FromQuery] bool aceita)
        {
            if (!_userCtx.IsAdmin(User))
                return Forbid("Apenas administradores podem confirmar/rejeitar solicitações.");

            var doacao = await _context.doacaoitem.FirstOrDefaultAsync(d => d.IdDoacaoItem == id);
            if (doacao == null) return NotFound();
            if (doacao.Status != DoacaoStatusEnum.Aguardando || doacao.RequeridoPor == null)
                return BadRequest("Doação não está em estado de solicitação pendente.");

            if (aceita)
            {
                doacao.Status = DoacaoStatusEnum.Solicitado;
            }
            else
            {
                doacao.Status = DoacaoStatusEnum.Disponivel;
                doacao.RequeridoPor = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Sucesso = true, Aceita = aceita, NovoStatus = doacao.Status.ToString() });
        }

        // PATCH: api/doacoes/{id}/status
        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult> AtualizarStatus(int id, [FromQuery] DoacaoStatusEnum novo)
        {
            // Admin sempre pode. Instituição pode alterar status se for atribuída. Doador não.
            var doacao = await _context.doacaoitem.FirstOrDefaultAsync(d => d.IdDoacaoItem == id);
            if (doacao == null) return NotFound();
            bool isInstituicao = _userCtx.IsInstituicao(User) && _userCtx.GetInstituicaoId(User) == doacao.IdInstituicaoAtribuida;
            if (!_userCtx.IsAdmin(User) && !isInstituicao)
                return Forbid("Sem permissão para alterar status.");

            doacao.Status = novo;
            await _context.SaveChangesAsync();
            return Ok(new { Sucesso = true });
        }
    }
}