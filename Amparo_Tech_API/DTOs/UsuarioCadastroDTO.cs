using System.ComponentModel.DataAnnotations;

namespace Amparo_Tech_API.DTOs
{
    public class UsuarioCadastroDTO : IValidatableObject
    {
        // Propriedades do usuário
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "O CPF deve conter 11 dígitos numéricos.")]
        public string Cpf { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail em formato inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        public string Senha { get; set; }

        [Required(ErrorMessage = "O tipo de usuário é obrigatório.")]
        public string TipoUsuario { get; set; }

        public string? Telefone { get; set; }

        // Propriedades do endereço (não obrigatórias por padrão)
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? InformacoesAdicionais { get; set; }

        // Validação condicional dos campos de endereço
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool algumEnderecoPreenchido =
                !string.IsNullOrWhiteSpace(Cep) ||
                !string.IsNullOrWhiteSpace(Logradouro) ||
                !string.IsNullOrWhiteSpace(Numero) ||
                !string.IsNullOrWhiteSpace(Cidade) ||
                !string.IsNullOrWhiteSpace(Estado);

            if (algumEnderecoPreenchido)
            {
                if (string.IsNullOrWhiteSpace(Cep))
                    yield return new ValidationResult("O CEP é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Cep) });

                if (string.IsNullOrWhiteSpace(Logradouro))
                    yield return new ValidationResult("O logradouro é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Logradouro) });

                if (string.IsNullOrWhiteSpace(Numero))
                    yield return new ValidationResult("O número é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Numero) });

                if (string.IsNullOrWhiteSpace(Cidade))
                    yield return new ValidationResult("A cidade é obrigatória quando qualquer campo de endereço é preenchido.", new[] { nameof(Cidade) });

                if (string.IsNullOrWhiteSpace(Estado))
                    yield return new ValidationResult("O estado é obrigatório quando qualquer campo de endereço é preenchido.", new[] { nameof(Estado) });
            }
        }
    }
}