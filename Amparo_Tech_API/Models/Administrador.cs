using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Amparo_Tech_API.Models
{
 [Table("administrador")]
 public class Administrador
 {
 [Key]
 [Column("idAdministrador")]
 public int IdAdministrador { get; set; }

 [Required]
 [Column("nome")]
 [StringLength(150)]
 public string Nome { get; set; }

 [Required]
 [Column("email")]
 [StringLength(200)]
 public string Email { get; set; }

 [Required]
 [Column("senha")]
 [StringLength(200)]
 public string Senha { get; set; }

 [Column("dataCadastro")]
 public DateTime? DataCadastro { get; set; }

 [Column("ultimoLogin")]
 public DateTime? UltimoLogin { get; set; }
 }
}
