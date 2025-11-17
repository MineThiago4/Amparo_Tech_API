using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Amparo_Tech_API.Models
{
    public enum TipoParticipanteMensagem
    {
        Usuario = 0,
        Instituicao = 1,
        Administrador = 2
    }

    [Index(nameof(IdRemetente), nameof(TipoRemetente))]
    [Index(nameof(IdDestinatario), nameof(TipoDestinatario))]
    [Table("mensagem")]
    public class Mensagem
    {
        [Key]
        [Column("idMensagem")]
        public int IdMensagem { get; set; }

        [Required]
        [Column("idRemetente")]
        public int IdRemetente { get; set; }

        [Required]
        [Column("tipoRemetente")]
        public TipoParticipanteMensagem TipoRemetente { get; set; } = TipoParticipanteMensagem.Usuario;

        [Required]
        [Column("idDestinatario")]
        public int IdDestinatario { get; set; }

        [Required]
        [Column("tipoDestinatario")]
        public TipoParticipanteMensagem TipoDestinatario { get; set; } = TipoParticipanteMensagem.Usuario;

        // agora opcional: mensagem pode não estar atrelada a uma doação
        [Column("idDoacaoItem")]
        public int? IdDoacaoItem { get; set; }

        [Required]
        [Column("conteudo")]
        public string Conteudo { get; set; }

        [Required]
        [Column("dataEnvio")]
        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;

        // Navegação
        [ForeignKey(nameof(IdDoacaoItem))]
        public DoacaoItem? Doacao { get; set; }
    }
}
