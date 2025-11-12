using Amparo_Tech_API.Data;
using Amparo_Tech_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Amparo_Tech_API.Services;
namespace Amparo_Tech_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userCtx;
        public CategoriasController(AppDbContext context, IUserContextService userCtx)
        {
            _context = context; _userCtx = userCtx;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Categoria>>> Get()
        {
            // Toca o serviço de contexto para futuras extensões de autorização sem alterar a resposta
            _ = _userCtx.GetUserId(User);
            return Ok(await _context.categoria.AsNoTracking().ToListAsync());
        }

        // Admin: criar categoria
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Categoria cat)
        {
            if (!_userCtx.IsAdmin(User)) return Forbid("Apenas administradores.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (await _context.categoria.AnyAsync(c => c.Nome == cat.Nome))
                return BadRequest("Nome de categoria já existente.");
            _context.categoria.Add(cat);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = cat.IdCategoria }, new { cat.IdCategoria, cat.Nome, cat.Descricao });
        }

        // Admin: atualizar categoria
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] Categoria cat)
        {
            if (!_userCtx.IsAdmin(User)) return Forbid("Apenas administradores.");
            var atual = await _context.categoria.FindAsync(id);
            if (atual == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(cat.Nome)) atual.Nome = cat.Nome;
            atual.Descricao = cat.Descricao;
            await _context.SaveChangesAsync();
            return Ok(new { atual.IdCategoria, atual.Nome, atual.Descricao });
        }

        // Admin: remover categoria (se não referenciada)
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (!_userCtx.IsAdmin(User)) return Forbid("Apenas administradores.");
            var atual = await _context.categoria.FindAsync(id);
            if (atual == null) return NotFound();
            var usada = await _context.doacaoitem.AnyAsync(d => d.IdCategoria == id);
            if (usada) return Conflict("Categoria está em uso por doações.");
            _context.categoria.Remove(atual);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}