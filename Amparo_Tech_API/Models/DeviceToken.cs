using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Amparo_Tech_API.Models
{
    [Table("devicetoken")]
    public class DeviceToken
    {
        [Key]
        [Column("idDeviceToken")]
        public int IdDeviceToken { get; set; }

        [Column("tipoOwner")]
        public TipoParticipanteMensagem TipoOwner { get; set; }

        [Column("idOwner")]
        public int IdOwner { get; set; }

        [Column("token")]
        [StringLength(500)]
        public string Token { get; set; }

        [Column("platform")]
        [StringLength(50)]
        public string? Platform { get; set; }

        [Column("dataCriacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
