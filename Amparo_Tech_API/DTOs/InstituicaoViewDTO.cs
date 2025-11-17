namespace Amparo_Tech_API.DTOs
{
 public class InstituicaoViewDTO
 {
 public int IdInstituicao { get; set; }
 public string Nome { get; set; }
 public string Email { get; set; }
 public string? Cnpj { get; set; }
 public string? Telefone { get; set; }
 public string? PessoaContato { get; set; }
 public EnderecoDTO? Endereco { get; set; }
 }
}
