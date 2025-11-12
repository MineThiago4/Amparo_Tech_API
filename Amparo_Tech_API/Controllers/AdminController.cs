using Amparo_Tech_API.Data;
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
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userCtx;
        public AdminController(AppDbContext context, IUserContextService userCtx)
        {
            _context = context; _userCtx = userCtx;
        }

        // GET: api/admin/resumo
        [HttpGet("resumo")]
        public async Task<ActionResult> ResumoGeral()
        {
            if (!_userCtx.IsAdmin(User))
                return Forbid("Apenas administradores.");

            var total = await _context.doacaoitem.CountAsync();
            var porStatus = await _context.doacaoitem
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Qtde = g.Count() })
                .ToListAsync();
            var pendentes = await _context.doacaoitem.CountAsync(d => d.Status == DoacaoStatusEnum.Solicitado && d.RequeridoPor != null);
            var ultimas = await _context.doacaoitem
                .OrderByDescending(d => d.DataDoacao)
                .Take(10)
                .Select(d => new { d.IdDoacaoItem, d.Titulo, d.Status, d.DataDoacao })
                .ToListAsync();
            var porCategoria = await _context.doacaoitem
                .GroupBy(d => d.IdCategoria)
                .Select(g => new { IdCategoria = g.Key, Qtde = g.Count() })
                .ToListAsync();

            return Ok(new { total, pendentes, porStatus, porCategoria, ultimas });
        }

        // GET: api/admin/instituicoes/{id}/resumo
        [HttpGet("instituicoes/{id:int}/resumo")]
        public async Task<ActionResult> ResumoInstituicao(int id)
        {
            if (!_userCtx.IsAdmin(User))
                return Forbid("Apenas administradores.");

            var existe = await _context.instituicao.AnyAsync(i => i.IdInstituicao == id);
            if (!existe) return NotFound("Instituição não encontrada.");

            var total = await _context.doacaoitem.CountAsync(d => d.IdInstituicaoAtribuida == id);
            var porStatus = await _context.doacaoitem
                .Where(d => d.IdInstituicaoAtribuida == id)
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Qtde = g.Count() })
                .ToListAsync();
            var pendentes = await _context.doacaoitem.CountAsync(d => d.IdInstituicaoAtribuida == id && d.Status == DoacaoStatusEnum.Solicitado && d.RequeridoPor != null);
            var ultimas = await _context.doacaoitem
                .Where(d => d.IdInstituicaoAtribuida == id)
                .OrderByDescending(d => d.DataDoacao)
                .Take(10)
                .Select(d => new { d.IdDoacaoItem, d.Titulo, d.Status, d.DataDoacao })
                .ToListAsync();

            return Ok(new { idInstituicao = id, total, pendentes, porStatus, ultimas });
        }

        // GET: api/admin/requeridas?instituicaoId=&page=&pageSize=
        [HttpGet("requeridas")]
        public async Task<ActionResult> ListarRequeridas([FromQuery] int? instituicaoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!_userCtx.IsAdmin(User))
                return Forbid("Apenas administradores.");

            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _context.doacaoitem.AsNoTracking()
                .Where(d => d.Status == DoacaoStatusEnum.Solicitado && d.RequeridoPor != null);
            if (instituicaoId.HasValue) q = q.Where(d => d.IdInstituicaoAtribuida == instituicaoId.Value);

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(d => d.DataDoacao)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(d => new { d.IdDoacaoItem, d.Titulo, d.RequeridoPor, d.IdInstituicaoAtribuida, d.Status, d.DataDoacao })
                .ToListAsync();

            return Ok(new { page, pageSize, total, items });
        }
    }
}
