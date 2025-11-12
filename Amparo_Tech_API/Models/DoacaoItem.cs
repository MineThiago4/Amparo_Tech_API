using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Amparo_Tech_API.Models
{
    [Table("doacaoitem")]
    public class DoacaoItem
    {
        [Key][Column("idDoacaoItem")] public int IdDoacaoItem { get; set; }
        [Required, Column("idDoador")]
        public int IdDoador { get; set; } // Usuariologin.IdUsuario

        [Required, Column("idCategoria")]
        public int IdCategoria { get; set; }

        [Required, Column("titulo")]
        [StringLength(120)]
        public string Titulo { get; set; }

        [Required, Column("descricao")]
        [StringLength(1000)]
        public string Descricao { get; set; }

        [Column("condicao")]
        [StringLength(200)]
        public string? Condicao { get; set; }

        // Armazena o ID opaco do arquivo salvo pelo endpoint de upload
        [Column("midiaId")]
        [StringLength(64)]
        public string? MidiaId { get; set; } // compat

        // navegação 1:N
        public ICollection<DoacaoMidia>? Midias { get; set; }

        // Nova atribuição: guarda o ID da instituição responsável
        [Column("idInstituicaoAtribuida")]
        public int? IdInstituicaoAtribuida { get; set; }

        [ForeignKey(nameof(IdInstituicaoAtribuida))]
        public Instituicao? InstituicaoAtribuida { get; set; }

        // Indica qual usuário beneficiário requisitou o item (para confirmação administrativa)
        [Column("requeridoPor")]
        public int? RequeridoPor { get; set; }

        [Column("dataDoacao")]
        public DateTime DataDoacao { get; set; } = DateTime.UtcNow;

        [Required, Column("status")]
        public DoacaoStatusEnum Status { get; set; } = DoacaoStatusEnum.Disponivel;
    }

    public enum DoacaoStatusEnum
    {
        Disponivel,
        Aguardando,
        Solicitado,
        Entregue,
        Arquivado
    }
}