using Amparo_Tech_API.Data;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Amparo_Tech_API.Controllers
{
    // Controller responsável pelos endpoints de usuários (CRUD, login, validações)
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        // Injeta o contexto do banco de dados
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // Cadastro de usuário (com hash de senha e validação de dados)
        [HttpPost]
        public async Task<ActionResult> PostUsuarioCompleto([FromBody] UsuarioCadastroDTO usuarioDto)
        {
            // Valida os dados recebidos
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verifica duplicidade de e-mail e CPF
            if (await _context.usuariologin.AnyAsync(u => u.Email == usuarioDto.Email))
                return BadRequest("E-mail já cadastrado.");
            if (await _context.usuariologin.AnyAsync(u => u.Cpf == usuarioDto.Cpf))
                return BadRequest("CPF já cadastrado.");

            // Valida o tipo de usuário
            if (!Enum.TryParse<TipoUsuarioEnum>(usuarioDto.TipoUsuario, out var tipoUsuario))
                return BadRequest("Tipo de usuário inválido.");

            try
            {
                // Cria endereço se algum campo foi preenchido
                Endereco endereco = null;
                bool algumEnderecoPreenchido =
                    !string.IsNullOrWhiteSpace(usuarioDto.Cep) ||
                    !string.IsNullOrWhiteSpace(usuarioDto.Logradouro) ||
                    !string.IsNullOrWhiteSpace(usuarioDto.Numero) ||
                    !string.IsNullOrWhiteSpace(usuarioDto.Cidade) ||
                    !string.IsNullOrWhiteSpace(usuarioDto.Estado);

                if (algumEnderecoPreenchido)
                {
                    endereco = new Endereco
                    {
                        Cep = usuarioDto.Cep,
                        Logradouro = usuarioDto.Logradouro,
                        Numero = usuarioDto.Numero,
                        Complemento = usuarioDto.Complemento,
                        Cidade = usuarioDto.Cidade,
                        Estado = usuarioDto.Estado,
                        InformacoesAdicionais = usuarioDto.InformacoesAdicionais
                    };
                }

                // Gera hash da senha
                var senhaHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Senha);

                // Cria usuário
                var usuario = new Usuariologin
                {
                    Nome = usuarioDto.Nome,
                    Cpf = usuarioDto.Cpf,
                    Email = usuarioDto.Email,
                    Senha = senhaHash,
                    TipoUsuario = tipoUsuario,
                    DataCadastro = DateTime.Now,
                    Telefone = usuarioDto.Telefone,
                    Endereco = endereco
                };

                // Salva usuário no banco
                _context.usuariologin.Add(usuario);
                await _context.SaveChangesAsync();

                // Retorna dados do usuário sem senha
                var usuarioRetorno = new
                {
                    usuario.IdUsuario,
                    usuario.Nome,
                    usuario.Cpf,
                    usuario.Email,
                    usuario.TipoUsuario,
                    usuario.DataCadastro,
                    usuario.Telefone,
                    Endereco = usuario.Endereco
                };

                return CreatedAtAction(nameof(PostUsuarioCompleto), new { id = usuario.IdUsuario }, usuarioRetorno);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Login do usuário (valida senha via hash)
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Busca usuário por e-mail
            var usuario = await _context.usuariologin
                .Include(u => u.Endereco)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (usuario == null)
                return Unauthorized("Usuário não encontrado.");

            // Verifica senha (hash)
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Senha, usuario.Senha))
                return Unauthorized("Senha incorreta.");

            // Retorna dados do usuário sem senha
            var usuarioRetorno = new
            {
                usuario.IdUsuario,
                usuario.Nome,
                usuario.Email,
                usuario.TipoUsuario,
                usuario.Telefone,
                usuario.Endereco
            };

            return Ok(usuarioRetorno);
        }

        // Lista todos os usuários (sem senha)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsuarios()
        {
            var usuarios = await _context.usuariologin
                .Include(u => u.Endereco)
                .Select(u => new
                {
                    u.IdUsuario,
                    u.Nome,
                    u.Cpf,
                    u.Email,
                    u.TipoUsuario,
                    u.DataCadastro,
                    u.Telefone,
                    u.Endereco
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // Busca usuário por ID (sem senha)
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUsuario(int id)
        {
            var usuario = await _context.usuariologin
                .Include(u => u.Endereco)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            var usuarioRetorno = new
            {
                usuario.IdUsuario,
                usuario.Nome,
                usuario.Cpf,
                usuario.Email,
                usuario.TipoUsuario,
                usuario.DataCadastro,
                usuario.Telefone,
                usuario.Endereco
            };

            return Ok(usuarioRetorno);
        }

        // Atualiza dados do usuário (mantém hash da senha)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuariologin usuario)
        {
            if (id != usuario.IdUsuario)
            {
                return BadRequest();
            }

            // Busca usuário atual
            var usuarioAtual = await _context.usuariologin.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuarioAtual == null)
            {
                return NotFound();
            }

            // Atualiza hash da senha se necessário
            if (!string.IsNullOrWhiteSpace(usuario.Senha) && usuario.Senha != usuarioAtual.Senha)
            {
                usuario.Senha = BCrypt.Net.BCrypt.HashPassword(usuario.Senha);
            }
            else
            {
                usuario.Senha = usuarioAtual.Senha;
            }

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.usuariologin.Any(e => e.IdUsuario == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Retorna dados do usuário sem senha
            var usuarioRetorno = new
            {
                usuario.IdUsuario,
                usuario.Nome,
                usuario.Cpf,
                usuario.Email,
                usuario.TipoUsuario,
                usuario.DataCadastro,
                usuario.Telefone,
                usuario.Endereco
            };

            return Ok(usuarioRetorno);
        }

        // Remove usuário
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.usuariologin.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            _context.usuariologin.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Verifica se e-mail já existe
        [HttpGet("email-existe")]
        public async Task<ActionResult<bool>> VerificarEmailExistente(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("O e-mail não pode ser vazio.");
            }

            var existe = await _context.usuariologin.AnyAsync(u => u.Email == email);
            return Ok(existe);
        }

        // Verifica se CPF já existe
        [HttpGet("cpf-existe")]
        public async Task<ActionResult<bool>> VerificarCpfExistente(string cpf)
        {
            if (string.IsNullOrEmpty(cpf))
            {
                return BadRequest("O CPF não pode ser vazio.");
            }

            var existe = await _context.usuariologin.AnyAsync(u => u.Cpf == cpf);
            return Ok(existe);
        }
    }
}