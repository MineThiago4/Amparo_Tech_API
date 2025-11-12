using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Amparo_Tech_API.DTOs
{
    public class UploadMidiaDTO
    {
        [Required]
        public IFormFile file { get; set; } = default!;
    }
}
