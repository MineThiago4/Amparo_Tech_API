namespace Amparo_Tech_API.DTOs
{
    public class NotificacaoDTO
    {
        public int IdNotificacao { get; set; }
        public string Titulo { get; set; }
        public string Conteudo { get; set; }
        public string? Link { get; set; }
        public int IdDestinatario { get; set; }
        public string TipoDestinatario { get; set; }
        public bool IsRead { get; set; }
        public string DataCriacao { get; set; }
    }
}
