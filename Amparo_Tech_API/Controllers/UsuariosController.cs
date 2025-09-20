using Amparo_Tech_API.Data;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        // Endpoint para Inserir(Create)
        [HttpPost]
        public async Task<ActionResult<Usuariologin>> PostUsuarioCompleto(UsuarioCadastroDTO usuarioDto)
        {
            try
            {
                Endereco endereco = null;
                if (!string.IsNullOrWhiteSpace(usuarioDto.Cep) || !string.IsNullOrWhiteSpace(usuarioDto.Logradouro))
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

                // Cria a instância de Usuariologin e associa o Endereco (que pode ser nulo)
                var usuario = new Usuariologin
                {
                    Nome = usuarioDto.Nome,
                    Cpf = usuarioDto.Cpf,
                    Email = usuarioDto.Email,
                    Senha = usuarioDto.Senha,
                    TipoUsuario = (TipoUsuarioEnum)Enum.Parse(typeof(TipoUsuarioEnum), usuarioDto.TipoUsuario),
                    DataCadastro = DateTime.Now,
                    Telefone = usuarioDto.Telefone,
                    Endereco = endereco // Associa a instância (pode ser null)
                };

                // Adiciona o usuário ao contexto. O Entity Framework Core
                // irá automaticamente detectar que o Endereco é um novo objeto
                // e irá inseri-lo primeiro, para depois inserir o usuário
                // e criar o relacionamento (ForeignKey).
                _context.usuariologin.Add(usuario);

                await _context.SaveChangesAsync();

                // Retorna o usuário criado.
                return CreatedAtAction(nameof(PostUsuarioCompleto), new { id = usuario.IdUsuario }, usuario);

            }
            catch (Exception ex)
            {
                // Em caso de erro, retorna um BadRequest com a mensagem de erro
                return BadRequest(new { message = ex.Message });
            }
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
                return BadRequest(); // Retorna 400 se o ID na URL não for igual ao ID do corpo
            }

            // Avisa o Entity Framework que o objeto foi modificado
            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Verifica se o usuário realmente existe antes de tentar salvar
                if (!_context.usuariologin.Any(e => e.IdUsuario == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Retorna 204 para indicar sucesso sem conteúdo de resposta
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