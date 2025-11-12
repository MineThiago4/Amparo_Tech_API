using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Amparo_Tech_API.Models
{
    [Table("doacaomidia")]
    public class DoacaoMidia
    {
        [Key]
        [Column("idDoacaoMidia")]
        public int IdDoacaoMidia { get; set; }

        [Required, Column("idDoacaoItem")]
        public int IdDoacaoItem { get; set; }

        // id retornado pelo UploadsController (GUID em string)
        [Required, Column("midiaId")]
        [StringLength(64)]
        public string MidiaId { get; set; }

        [Required, Column("tipo")] // "Imagem" ou "Video"
        [StringLength(20)]
        public string Tipo { get; set; }

        [Column("ordem")]
        public int? Ordem { get; set; }

        [ForeignKey(nameof(IdDoacaoItem))]
        public DoacaoItem DoacaoItem { get; set; }
    }
}