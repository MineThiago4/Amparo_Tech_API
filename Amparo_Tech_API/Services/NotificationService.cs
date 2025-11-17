using Amparo_Tech_API.Data;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Amparo_Tech_API.Hubs;
using Google.Apis.Auth.OAuth2;
using System.Net.Http.Headers;

namespace Amparo_Tech_API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IServiceProvider _sp;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IConfiguration _cfg;
        public NotificationService(AppDbContext db, IServiceProvider sp, IHubContext<NotificationHub> hub, IConfiguration cfg)
        {
            _db = db; _sp = sp; _hub = hub; _cfg = cfg;
        }

        public async Task SendNotificationAsync(NotificacaoDTO dto)
        {
            var tipo = Enum.Parse<TipoParticipanteMensagem>(dto.TipoDestinatario);
            var n = new Notificacao
            {
                Titulo = dto.Titulo,
                Conteudo = dto.Conteudo,
                Link = dto.Link,
                TipoDestinatario = tipo,
                IdDestinatario = dto.IdDestinatario,
                Data = dto.DataCriacao,
                DataCriacao = DateTime.UtcNow
            };
            _db.notificacao.Add(n);
            await _db.SaveChangesAsync();

            // send push if device token registered
            await SendPushAsync(dto.IdDestinatario, tipo, dto.Titulo, dto.Conteudo, new { link = dto.Link, id = n.IdNotificacao });

            // send to SignalR group
            try
            {
                var group = tipo == TipoParticipanteMensagem.Usuario ? $"user:{dto.IdDestinatario}" : (tipo == TipoParticipanteMensagem.Instituicao ? $"instituicao:{dto.IdDestinatario}" : $"admin:{dto.IdDestinatario}");
                await _hub.Clients.Group(group).SendAsync("notification", new { id = n.IdNotificacao, title = n.Titulo, body = n.Conteudo, link = n.Link });
            }
            catch { }
        }

        public async Task SendPushAsync(int idDestinatario, TipoParticipanteMensagem tipo, string title, string body, object? payload = null)
        {
            var tokens = await _db.devicetoken.AsNoTracking().Where(t => t.IdOwner == idDestinatario && t.TipoOwner == tipo).ToListAsync();
            if (tokens.Count == 0) return;

            // prefer HTTP v1 using service account
            var saPath = _cfg["Push:FCM:ServiceAccountPath"];
            var projectId = _cfg["Push:FCM:ProjectId"] ?? _cfg["Firebase:ProjectId"];
            if (!string.IsNullOrWhiteSpace(saPath) && !string.IsNullOrWhiteSpace(projectId) && System.IO.File.Exists(saPath))
            {
                GoogleCredential cred;
                try
                {
                    cred = GoogleCredential.FromFile(saPath).CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                }
                catch
                {
                    cred = null;
                }
                if (cred != null)
                {
                    var accessToken = await cred.UnderlyingCredential.GetAccessTokenForRequestAsync();
                    var endpoint = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    foreach (var d in tokens)
                    {
                        var message = new
                        {
                            message = new
                            {
                                token = d.Token,
                                notification = new { title, body },
                                data = payload ?? new { }
                            }
                        };
                        var json = JsonSerializer.Serialize(message);
                        await http.PostAsync(endpoint, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                    }
                    return;
                }
            }

            // fallback: legacy server key (not preferred)
            var fcmKey = _cfg?["Push:FCM:ServerKey"];
            if (string.IsNullOrWhiteSpace(fcmKey)) return;
            using var http2 = new HttpClient();
            http2.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "key=" + fcmKey);
            var fcmEndpoint = "https://fcm.googleapis.com/fcm/send";
            foreach (var d in tokens)
            {
                var message = new
                {
                    to = d.Token,
                    notification = new { title, body },
                    data = payload ?? new { }
                };
                var json = JsonSerializer.Serialize(message);
                await http2.PostAsync(fcmEndpoint, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
            }
        }

        public async Task SendPushToTokenAsync(string token, string title, string body, object? payload = null)
        {
            if (string.IsNullOrWhiteSpace(token)) return;
            // prefer HTTP v1
            var saPath = _cfg["Push:FCM:ServiceAccountPath"];
            var projectId = _cfg["Push:FCM:ProjectId"] ?? _cfg["Firebase:ProjectId"];
            if (!string.IsNullOrWhiteSpace(saPath) && !string.IsNullOrWhiteSpace(projectId) && System.IO.File.Exists(saPath))
            {
                GoogleCredential cred;
                try
                {
                    cred = GoogleCredential.FromFile(saPath).CreateScoped("https://www.googleapis.com/auth/cloud-platform");
                }
                catch
                {
                    cred = null;
                }
                if (cred != null)
                {
                    var accessToken = await cred.UnderlyingCredential.GetAccessTokenForRequestAsync();
                    var endpoint = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var message = new
                    {
                        message = new
                        {
                            token = token,
                            notification = new { title, body },
                            data = payload ?? new { }
                        }
                    };
                    var json = JsonSerializer.Serialize(message);
                    await http.PostAsync(endpoint, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                    return;
                }
            }

            var fcmKey = _cfg?["Push:FCM:ServerKey"];
            if (string.IsNullOrWhiteSpace(fcmKey)) return;
            using var http2 = new HttpClient();
            http2.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "key=" + fcmKey);
            var fcmEndpoint = "https://fcm.googleapis.com/fcm/send";
            var message2 = new
            {
                to = token,
                notification = new { title, body },
                data = payload ?? new { }
            };
            var json2 = JsonSerializer.Serialize(message2);
            await http2.PostAsync(fcmEndpoint, new StringContent(json2, System.Text.Encoding.UTF8, "application/json"));
        }
    }
}
