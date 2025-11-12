using System.ComponentModel.DataAnnotations;

namespace Amparo_Tech_API.DTOs
{
    public class SolicitacaoDoacaoDTO
    {
        [Required]
        public string Mensagem { get; set; }
    }
}
