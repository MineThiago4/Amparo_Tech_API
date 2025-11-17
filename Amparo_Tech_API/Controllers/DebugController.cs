using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amparo_Tech_API.Controllers
{
    [ApiController]
    [Route("debug")]
    [Authorize]
    public class DebugController : ControllerBase
    {
        private readonly INotificationService _notify;
        public DebugController(INotificationService notify) { _notify = notify; }

        [HttpPost("sendpush")]
        public async Task<IActionResult> SendPush([FromBody] DebugPushDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            // only admin allowed
            var isAdmin = User?.Claims?.Any(c => c.Type == "tipoUsuario" && c.Value == "Administrador") ?? false;
            if (!isAdmin) return Forbid();

            await _notify.SendPushToTokenAsync(dto.Token, dto.Title, dto.Body, dto.Data);
            return Ok(new { Sucesso = true, detail = "request submitted" });
        }
    }
}
