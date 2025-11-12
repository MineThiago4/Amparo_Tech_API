using System.ComponentModel.DataAnnotations;

namespace Amparo_Tech_API.DTOs
{
 public class AdminAtualizacaoDTO
 {
 [StringLength(150)]
 public string? Nome { get; set; }
 [EmailAddress, StringLength(200)]
 public string? Email { get; set; }
 [StringLength(100, MinimumLength =6)]
 public string? NovaSenha { get; set; }
 }
}
