using System.ComponentModel.DataAnnotations;
using Amparo_Tech_API.Models;

namespace Amparo_Tech_API.DTOs
{
    public class MensagemEnvioDTO
    {
        // now optional: message may not be tied to a donation
        public int? IdDoacaoItem { get; set; }

        [Required]
        public int IdDestinatario { get; set; }

        [Required]
        public TipoParticipanteMensagem TipoDestinatario { get; set; }

        [Required]
        [StringLength(2000)]
        public string Conteudo { get; set; }
    }
}
