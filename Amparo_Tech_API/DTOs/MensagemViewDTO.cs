using System;

namespace Amparo_Tech_API.DTOs
{
    public class MensagemViewDTO
    {
        public int IdMensagem { get; set; }
        public int IdRemetente { get; set; }
        public string TipoRemetente { get; set; }
        public int IdDestinatario { get; set; }
        public string TipoDestinatario { get; set; }
        public int? IdDoacaoItem { get; set; }
        public string Conteudo { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}
