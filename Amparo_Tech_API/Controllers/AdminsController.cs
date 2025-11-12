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
 public class AdminsController : ControllerBase
 {
 private readonly AppDbContext _context;
 private readonly IUserContextService _userCtx;
 public AdminsController(AppDbContext context, IUserContextService userCtx) { _context = context; _userCtx = userCtx; }

 private bool CheckAdmin() => _userCtx.IsAdmin(User);

 [HttpGet]
 public async Task<ActionResult> Listar()
 {
 if (!CheckAdmin()) return Forbid("Apenas administradores.");
 var admins = await _context.administrador.AsNoTracking()
 .Select(a => new { a.IdAdministrador, a.Nome, a.Email, a.DataCadastro, a.UltimoLogin })
 .ToListAsync();
 return Ok(admins);
 }

 [HttpGet("{id:int}")]
 public async Task<ActionResult> Obter(int id)
 {
 if (!CheckAdmin()) return Forbid("Apenas administradores.");
 var a = await _context.administrador.AsNoTracking().FirstOrDefaultAsync(x => x.IdAdministrador == id);
 if (a == null) return NotFound();
 return Ok(new { a.IdAdministrador, a.Nome, a.Email, a.DataCadastro, a.UltimoLogin });
 }

 [HttpPost]
 public async Task<ActionResult> Criar([FromBody] AdminCadastroDTO dto)
 {
 if (!CheckAdmin()) return Forbid("Apenas administradores.");
 if (!ModelState.IsValid) return BadRequest(ModelState);
 if (await _context.administrador.AnyAsync(a => a.Email == dto.Email)) return BadRequest("E-mail já cadastrado.");
 var admin = new Administrador
 {
 Nome = dto.Nome,
 Email = dto.Email,
 Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
 DataCadastro = DateTime.UtcNow
 };
 _context.administrador.Add(admin);
 await _context.SaveChangesAsync();
 return CreatedAtAction(nameof(Obter), new { id = admin.IdAdministrador }, new { admin.IdAdministrador, admin.Nome, admin.Email });
 }

 [HttpPut("{id:int}")]
 public async Task<ActionResult> Atualizar(int id, [FromBody] AdminAtualizacaoDTO dto)
 {
 if (!CheckAdmin()) return Forbid("Apenas administradores.");
 var a = await _context.administrador.FirstOrDefaultAsync(x => x.IdAdministrador == id);
 if (a == null) return NotFound();
 if (!string.IsNullOrWhiteSpace(dto.Nome)) a.Nome = dto.Nome;
 if (!string.IsNullOrWhiteSpace(dto.Email))
 {
 if (await _context.administrador.AnyAsync(x => x.Email == dto.Email && x.IdAdministrador != id))
 return BadRequest("E-mail já em uso.");
 a.Email = dto.Email;
 }
 if (!string.IsNullOrWhiteSpace(dto.NovaSenha)) a.Senha = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
 await _context.SaveChangesAsync();
 return Ok(new { a.IdAdministrador, a.Nome, a.Email });
 }

 [HttpDelete("{id:int}")]
 public async Task<ActionResult> Remover(int id)
 {
 if (!CheckAdmin()) return Forbid("Apenas administradores.");
 var a = await _context.administrador.FindAsync(id);
 if (a == null) return NotFound();
 _context.administrador.Remove(a);
 await _context.SaveChangesAsync();
 return NoContent();
 }
 }
}
