using System.ComponentModel.DataAnnotations;

namespace Amparo_Tech_API.DTOs
{
 public class AdminCadastroDTO
 {
 [Required, StringLength(150)]
 public string Nome { get; set; }
 [Required, EmailAddress, StringLength(200)]
 public string Email { get; set; }
 [Required, StringLength(100, MinimumLength =6)]
 public string Senha { get; set; }
 }
}
