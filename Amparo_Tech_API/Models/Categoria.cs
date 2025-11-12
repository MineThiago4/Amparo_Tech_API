using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Amparo_Tech_API.Models
{
    [Table("categoria")]
    public class Categoria
    {
        [Key][Column("idCategoria")] public int IdCategoria { get; set; }
        [Required, Column("nome")]
        [StringLength(100)]
        public string Nome { get; set; }

        [Column("descricao")]
        [StringLength(500)]
        public string? Descricao { get; set; }
    }
}