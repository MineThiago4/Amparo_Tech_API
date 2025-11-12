using Amparo_Tech_API.Data;
using Amparo_Tech_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Amparo_Tech_API.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Amparo_Tech_API.DTOs;
using System.Text;

namespace Amparo_Tech_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InstituicoesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userCtx;
        private readonly IConfiguration _cfg;
        public InstituicoesController(AppDbContext context, IUserContextService userCtx, IConfiguration cfg)
        {
            _context = context; _userCtx = userCtx; _cfg = cfg;
        }
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            _ = _userCtx.GetUserId(User);
            var lista = await _context.instituicao
                .Include(i => i.Endereco)
                .AsNoTracking()
                .Select(i => new {
                    i.IdInstituicao,
                    i.Nome,
                    i.Email,
                    i.Cnpj,
                    i.Telefone,
                    i.PessoaContato,
                    i.DataCadastro,
                    Endereco = i.Endereco
                })
                .ToListAsync();
            return Ok(lista);
        }

        // Criar instituição (admin) com endereço opcional
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] InstituicaoCadastroDTO dto)
        {
            if (!_userCtx.IsAdmin(User)) return Forbid("Apenas administradores.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (await _context.instituicao.AnyAsync(i => i.Email == dto.Email)) return BadRequest("Email já cadastrado.");
            Endereco? endereco = null;
            bool anyEndereco = !string.IsNullOrWhiteSpace(dto.Cep) || !string.IsNullOrWhiteSpace(dto.Logradouro) || !string.IsNullOrWhiteSpace(dto.Numero) || !string.IsNullOrWhiteSpace(dto.Cidade) || !string.IsNullOrWhiteSpace(dto.Estado);
            if (anyEndereco)
            {
                endereco = new Endereco
                {
                    Cep = dto.Cep ?? string.Empty,
                    Logradouro = dto.Logradouro ?? string.Empty,
                    Numero = dto.Numero ?? string.Empty,
                    Complemento = dto.Complemento ?? string.Empty,
                    Cidade = dto.Cidade ?? string.Empty,
                    Estado = dto.Estado ?? string.Empty,
                    InformacoesAdicionais = dto.InformacoesAdicionais
                };
                _context.endereco.Add(endereco);
            }
            var inst = new Instituicao
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Cnpj = dto.Cnpj,
                Telefone = dto.Telefone,
                PessoaContato = dto.PessoaContato,
                Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                DataCadastro = DateTime.UtcNow,
                Endereco = endereco
            };
            _context.instituicao.Add(inst);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = inst.IdInstituicao }, new { inst.IdInstituicao, inst.Nome, inst.Email });
        }

        // Atualizar instituição (admin ou própria instituição) incluindo endereço
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] InstituicaoAtualizacaoDTO dto)
        {
            var inst = await _context.instituicao.Include(i => i.Endereco).FirstOrDefaultAsync(i => i.IdInstituicao == id);
            if (inst == null) return NotFound();
            bool isSelf = _userCtx.IsInstituicao(User) && _userCtx.GetInstituicaoId(User) == id;
            bool isAdmin = _userCtx.IsAdmin(User);
            if (!isSelf && !isAdmin) return Forbid("Sem permissão.");

            if (!string.IsNullOrWhiteSpace(dto.Nome)) inst.Nome = dto.Nome;
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (await _context.instituicao.AnyAsync(x => x.Email == dto.Email && x.IdInstituicao != id)) return BadRequest("Email já em uso.");
                inst.Email = dto.Email;
            }
            if (!string.IsNullOrWhiteSpace(dto.Cnpj)) inst.Cnpj = dto.Cnpj;
            if (!string.IsNullOrWhiteSpace(dto.Telefone)) inst.Telefone = dto.Telefone;
            if (!string.IsNullOrWhiteSpace(dto.PessoaContato)) inst.PessoaContato = dto.PessoaContato;
            if (isAdmin && !string.IsNullOrWhiteSpace(dto.NovaSenha)) inst.Senha = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);

            bool anyEndereco = !string.IsNullOrWhiteSpace(dto.Cep) || !string.IsNullOrWhiteSpace(dto.Logradouro) || !string.IsNullOrWhiteSpace(dto.Numero) || !string.IsNullOrWhiteSpace(dto.Cidade) || !string.IsNullOrWhiteSpace(dto.Estado);
            if (anyEndereco)
            {
                if (inst.Endereco == null)
                {
                    inst.Endereco = new Endereco();
                    _context.endereco.Add(inst.Endereco);
                }
                inst.Endereco.Cep = dto.Cep ?? inst.Endereco.Cep;
                inst.Endereco.Logradouro = dto.Logradouro ?? inst.Endereco.Logradouro;
                inst.Endereco.Numero = dto.Numero ?? inst.Endereco.Numero;
                inst.Endereco.Complemento = dto.Complemento ?? inst.Endereco.Complemento;
                inst.Endereco.Cidade = dto.Cidade ?? inst.Endereco.Cidade;
                inst.Endereco.Estado = dto.Estado ?? inst.Endereco.Estado;
                inst.Endereco.InformacoesAdicionais = dto.InformacoesAdicionais ?? inst.Endereco.InformacoesAdicionais;
            }

            await _context.SaveChangesAsync();
            return Ok(new { inst.IdInstituicao, inst.Nome, inst.Email });
        }

        // Remover instituição (admin)
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (!_userCtx.IsAdmin(User)) return Forbid("Apenas administradores.");
            var inst = await _context.instituicao.Include(i => i.Endereco).FirstOrDefaultAsync(i => i.IdInstituicao == id);
            if (inst == null) return NotFound();
            var temDoacoes = await _context.doacaoitem.AnyAsync(d => d.IdInstituicaoAtribuida == id);
            if (temDoacoes) return Conflict("Instituição possui doações atribuídas.");
            if (inst.Endereco != null)
            {
                _context.endereco.Remove(inst.Endereco);
            }
            _context.instituicao.Remove(inst);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Login de instituição
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> LoginInstituicao([FromBody] InstituicaoLoginDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var inst = await _context.instituicao.Include(i => i.Endereco).FirstOrDefaultAsync(i => i.Email == dto.Email);
            if (inst == null) return Unauthorized("Instituição não encontrada.");
            if (string.IsNullOrWhiteSpace(inst.Senha) || !BCrypt.Net.BCrypt.Verify(dto.Senha, inst.Senha))
                return Unauthorized("Senha incorreta.");
            inst.UltimoLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            var token = GerarTokenInstituicao(inst);
            return Ok(new { token, tipo = "Instituicao" });
        }

        private string GerarTokenInstituicao(Instituicao inst)
        {
            var keyString = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado");
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim("idInstituicao", inst.IdInstituicao.ToString()),
                new Claim(ClaimTypes.Email, inst.Email),
                new Claim(ClaimTypes.Name, inst.Nome ?? string.Empty),
                new Claim("tipoUsuario", "Instituicao")
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