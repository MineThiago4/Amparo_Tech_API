using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Amparo_Tech_API.Services;
using Amparo_Tech_API.Data;

namespace Amparo_Tech_API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IUserContextService _userCtx;
        public NotificationHub(IUserContextService userCtx)
        {
            _userCtx = userCtx;
        }

        public override async Task OnConnectedAsync()
        {
            // join groups based on user type and id so server can push to specific users/institutions/admins
            if (_userCtx.TryGetUserId(Context.User, out var uid))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{uid}");
            }
            if (_userCtx.GetInstituicaoId(Context.User) is int iid)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"instituicao:{iid}");
            }
            if (_userCtx.GetAdministradorId(Context.User) is int aid)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"admin:{aid}");
            }
            await base.OnConnectedAsync();
        }
    }
}
