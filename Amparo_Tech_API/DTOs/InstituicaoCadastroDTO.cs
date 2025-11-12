using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Amparo_Tech_API.DTOs
{
 public class InstituicaoCadastroDTO : IValidatableObject
 {
 [Required, StringLength(150)] public string Nome { get; set; }
 [EmailAddress, Required, StringLength(200)] public string Email { get; set; }
 [StringLength(18)] public string? Cnpj { get; set; }
 [StringLength(30)] public string? Telefone { get; set; }
 [StringLength(120)] public string? PessoaContato { get; set; }
 [Required, StringLength(100, MinimumLength =6)] public string Senha { get; set; }
 // Endereço opcional
 public string? Cep { get; set; }
 public string? Logradouro { get; set; }
 public string? Numero { get; set; }
 public string? Complemento { get; set; }
 public string? Cidade { get; set; }
 public string? Estado { get; set; }
 public string? InformacoesAdicionais { get; set; }

 public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
 {
 // CNPJ (somente dígitos ou com máscara). Aceita vazio.
 if (!string.IsNullOrWhiteSpace(Cnpj))
 {
 var digits = Regex.Replace(Cnpj, "[^0-9]", "");
 if (digits.Length !=14)
 yield return new ValidationResult("O CNPJ deve conter14 dígitos numéricos.", new[] { nameof(Cnpj) });
 }

 bool anyEndereco = !string.IsNullOrWhiteSpace(Cep) ||
 !string.IsNullOrWhiteSpace(Logradouro) ||
 !string.IsNullOrWhiteSpace(Numero) ||
 !string.IsNullOrWhiteSpace(Complemento) ||
 !string.IsNullOrWhiteSpace(Cidade) ||
 !string.IsNullOrWhiteSpace(Estado);
 if (anyEndereco)
 {
 if (string.IsNullOrWhiteSpace(Cep)) yield return new ValidationResult("O CEP é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Cep) });
 if (string.IsNullOrWhiteSpace(Logradouro)) yield return new ValidationResult("O logradouro é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Logradouro) });
 if (string.IsNullOrWhiteSpace(Numero)) yield return new ValidationResult("O número é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Numero) });
 if (string.IsNullOrWhiteSpace(Complemento)) yield return new ValidationResult("O complemento é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Complemento) });
 if (string.IsNullOrWhiteSpace(Cidade)) yield return new ValidationResult("A cidade é obrigatória quando qualquer campo de endereço é preenchido.", new[] { nameof(Cidade) });
 if (string.IsNullOrWhiteSpace(Estado)) yield return new ValidationResult("O estado é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Estado) });
 }
 }
 }
}
