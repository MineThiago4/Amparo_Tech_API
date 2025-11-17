using System.ComponentModel.DataAnnotations;

namespace Amparo_Tech_API.DTOs
{
    public class DebugPushDTO
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Body { get; set; }
        public object? Data { get; set; }
    }
}
