using Amparo_Tech_API.Data;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Amparo_Tech_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // Cadastro de usuário com hash de senha
        [HttpPost]
        public async Task<ActionResult> PostUsuarioCompleto([FromBody] UsuarioCadastroDTO usuarioDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.usuariologin.AnyAsync(u => u.Email == usuarioDto.Email))
                return BadRequest("E-mail já cadastrado.");

            if (await _context.usuariologin.AnyAsync(u => u.Cpf == usuarioDto.Cpf))
                return BadRequest("CPF já cadastrado.");

            if (!Enum.TryParse<TipoUsuarioEnum>(usuarioDto.TipoUsuario, out var tipoUsuario))
                return BadRequest("Tipo de usuário inválido.");

            try
            {
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

                // Hash da senha
                var senhaHash = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Senha);

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

                _context.usuariologin.Add(usuario);
                await _context.SaveChangesAsync();

                // Retorno seguro, sem senha
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

        // Login seguro
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _context.usuariologin
                .Include(u => u.Endereco)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (usuario == null)
                return Unauthorized("Usuário não encontrado.");

            // Verifica o hash da senha
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Senha, usuario.Senha))
                return Unauthorized("Senha incorreta.");

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

        // Endpoint para Visualizar (Read) - Lista todos os usuários
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuariologin>>> GetUsuarios()
        {
            // Retorna a lista de todos os usuários com seus endereços relacionados
            return await _context.usuariologin.Include(u => u.Endereco).ToListAsync();
        }

        // Endpoint para Visualizar (Read) - Pega um usuário por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuariologin>> GetUsuario(int id)
        {
            // Procura o usuário no banco de dados e inclui o endereço
            var usuario = await _context.usuariologin.Include(u => u.Endereco).FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound(); // Retorna 404 se o usuário não for encontrado
            }

            return usuario;
        }

        // Endpoint para Alterar (Update)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuariologin usuario)
        {
            if (id != usuario.IdUsuario)
            {
                return BadRequest();
            }

            // Busca o usuário atual no banco
            var usuarioAtual = await _context.usuariologin.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuarioAtual == null)
            {
                return NotFound();
            }

            // Se a senha foi alterada, gera o hash
            if (!string.IsNullOrWhiteSpace(usuario.Senha) && usuario.Senha != usuarioAtual.Senha)
            {
                usuario.Senha = BCrypt.Net.BCrypt.HashPassword(usuario.Senha);
            }
            else
            {
                // Mantém o hash existente se não foi alterada
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

            return NoContent();
        }

        // Endpoint para Apagar (Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.usuariologin.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(); // Retorna 404 se o usuário não for encontrado
            }

            _context.usuariologin.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent(); // Retorna 204 para indicar sucesso
        }

        // Endpoint para verificar se um e-mail já existe
        [HttpGet("email-existe")]
        public async Task<ActionResult<bool>> VerificarEmailExistente(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("O e-mail não pode ser vazio.");
            }

            // Verifica se existe algum usuário com o e-mail fornecido
            var existe = await _context.usuariologin.AnyAsync(u => u.Email == email);

            return Ok(existe); // Retorna 'true' se existir, 'false' se não
        }

        // Endpoint para verificar se um CPF já existe
        [HttpGet("cpf-existe")]
        public async Task<ActionResult<bool>> VerificarCpfExistente(string cpf)
        {
            if (string.IsNullOrEmpty(cpf))
            {
                return BadRequest("O CPF não pode ser vazio.");
            }

            // Verifica se existe algum usuário com o CPF fornecido
            var existe = await _context.usuariologin.AnyAsync(u => u.Cpf == cpf);

            return Ok(existe); // Retorna 'true' se existir, 'false' se não
        }
    }
}