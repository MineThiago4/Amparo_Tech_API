using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace Amparo_Tech_API.Services
{
    public class DefaultUserContextService : IUserContextService
    {
        public int? GetUserId(ClaimsPrincipal user)
        {
            if (user == null) return null;
            var candidates = new[] { "idUsuario", "idusuario", ClaimTypes.NameIdentifier, "sub" };
            foreach (var t in candidates)
            {
                var v = user.FindFirst(t)?.Value;
                if (int.TryParse(v, out var id)) return id;
            }
            return null;
        }

        public bool TryGetUserId(ClaimsPrincipal user, out int id)
        {
            id = GetUserId(user) ?? 0;
            return id > 0;
        }

        public string? GetTipoUsuario(ClaimsPrincipal user)
            => user?.FindFirst("tipoUsuario")?.Value;

        public bool IsAdmin(ClaimsPrincipal user)
            => string.Equals(GetTipoUsuario(user), "Administrador", StringComparison.OrdinalIgnoreCase) || GetAdministradorId(user).HasValue;

        public bool IsBeneficiario(ClaimsPrincipal user)
            => string.Equals(GetTipoUsuario(user), "Beneficiário", StringComparison.OrdinalIgnoreCase);

        public bool IsDoador(ClaimsPrincipal user)
            => string.Equals(GetTipoUsuario(user), "Doador", StringComparison.OrdinalIgnoreCase);

        public bool IsInstituicao(ClaimsPrincipal user)
            => string.Equals(GetTipoUsuario(user), "Instituicao", StringComparison.OrdinalIgnoreCase) || GetInstituicaoId(user).HasValue;

        public int? GetInstituicaoId(ClaimsPrincipal user)
        {
            var v = user?.FindFirst("idInstituicao")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        public int? GetAdministradorId(ClaimsPrincipal user)
        {
            var v = user?.FindFirst("idAdministrador")?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }

        public string? GetEmail(ClaimsPrincipal user)
            => user?.FindFirst(ClaimTypes.Email)?.Value ?? user?.FindFirst("email")?.Value;

        public string? GetNome(ClaimsPrincipal user)
            => user?.Identity?.Name;

        public DateTime? GetExpUtc(ClaimsPrincipal user)
        {
            var expStr = GetClaim(user, "exp");
            if (long.TryParse(expStr, out var seconds))
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            return null;
        }

        public IEnumerable<string> GetRoles(ClaimsPrincipal user)
            => user?.FindAll(ClaimTypes.Role).Select(r => r.Value) ?? Enumerable.Empty<string>();

        public string? GetClaim(ClaimsPrincipal user, string type) => user?.FindFirst(type)?.Value;
    }
}
