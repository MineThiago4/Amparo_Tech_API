using System.Security.Claims;

namespace Amparo_Tech_API.Services
{
    public interface IUserContextService
    {
        int? GetUserId(ClaimsPrincipal user);
        bool TryGetUserId(ClaimsPrincipal user, out int id);
        string? GetTipoUsuario(ClaimsPrincipal user);
        bool IsAdmin(ClaimsPrincipal user);
        bool IsBeneficiario(ClaimsPrincipal user);
        bool IsDoador(ClaimsPrincipal user);
        bool IsInstituicao(ClaimsPrincipal user);
        int? GetInstituicaoId(ClaimsPrincipal user);
        int? GetAdministradorId(ClaimsPrincipal user);
        string? GetEmail(ClaimsPrincipal user);
        string? GetNome(ClaimsPrincipal user);
        DateTime? GetExpUtc(ClaimsPrincipal user);
        IEnumerable<string> GetRoles(ClaimsPrincipal user);
        string? GetClaim(ClaimsPrincipal user, string type);
    }
}
