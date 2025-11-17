using System.ComponentModel.DataAnnotations;

namespace Amparo_Tech_API.DTOs
{
    public class DeviceTokenDTO
    {
        [Required]
        public string Token { get; set; }

        // "Usuario", "Instituicao", "Administrador"
        public string? TipoOwner { get; set; }

        public int? IdOwner { get; set; }

        public string? Platform { get; set; }
    }
}
