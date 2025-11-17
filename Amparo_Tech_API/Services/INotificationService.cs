using Amparo_Tech_API.Models;
using Amparo_Tech_API.DTOs;
namespace Amparo_Tech_API.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(NotificacaoDTO dto);
        Task SendPushAsync(int idDestinatario, TipoParticipanteMensagem tipo, string title, string body, object? payload = null);
        Task SendPushToTokenAsync(string token, string title, string body, object? payload = null);
    }
}
