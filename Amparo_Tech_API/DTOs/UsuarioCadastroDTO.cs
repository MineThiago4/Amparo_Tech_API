namespace Amparo_Tech_API.DTOs
{
    public class UsuarioCadastroDTO
    {
        // Propriedades do usuário
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string TipoUsuario { get; set; } // Ou o seu enum
        public string? Telefone { get; set; }

        // Propriedades do endereço
        public string Cep { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string? InformacoesAdicionais { get; set; }
    }
}