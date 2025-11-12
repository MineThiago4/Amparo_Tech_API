using System.ComponentModel.DataAnnotations;

namespace Amparo_Tech_API.DTOs
{
 public class InstituicaoLoginDTO
 {
 [Required]
 public string Email { get; set; }
 [Required]
 public string Senha { get; set; }
 }
}
