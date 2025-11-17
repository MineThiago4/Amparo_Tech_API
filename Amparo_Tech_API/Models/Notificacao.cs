using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Amparo_Tech_API.Models
{
    [Table("notificacao")]
    public class Notificacao
    {
        [Key]
        [Column("idNotificacao")]
        public int IdNotificacao { get; set; }

        [Column("titulo")]
        [StringLength(250)]
        public string Titulo { get; set; }

        [Column("conteudo")]
        public string Conteudo { get; set; }

        // destinatário: tipo (Usuario, Instituicao, Administrador) e id
        [Column("tipoDestinatario")]
        public TipoParticipanteMensagem TipoDestinatario { get; set; }

        [Column("idDestinatario")]
        public int IdDestinatario { get; set; }

        [Column("tipoRemetente")]
        public TipoParticipanteMensagem? TipoRemetente { get; set; }

        [Column("idRemetente")]
        public int? IdRemetente { get; set; }

        [Column("link")]
        [StringLength(500)]
        public string? Link { get; set; }

        [Column("data")]
        public string? Data { get; set; } // opcional JSON com payload

        [Column("isRead")]
        public bool IsRead { get; set; } = false;

        [Column("dataCriacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
