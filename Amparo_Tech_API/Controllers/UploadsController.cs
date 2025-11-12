using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Amparo_Tech_API.DTOs;
using Amparo_Tech_API.Services;
namespace Amparo_Tech_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UploadsController : ControllerBase
    {
        private readonly IMediaPolicyService _media;
        public UploadsController(IMediaPolicyService media) => _media = media;

        [HttpPost("midia")]
        [RequestSizeLimit(50L * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMidia([FromForm] UploadMidiaDTO model, CancellationToken ct)
        {
            var file = model.file;
            if (file == null || file.Length == 0) return BadRequest("Arquivo não enviado.");
            if (file.Length > _media.MaxBytes) return BadRequest("Arquivo excede o limite de 50MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext) || !_media.AllowedExtensions.Contains(ext))
                return BadRequest("Tipo de arquivo não permitido.");

            var id = Guid.NewGuid().ToString("N");
            var saveName = id + ext;
            var savePath = Path.Combine(_media.GetStorageRoot(), saveName);

            await using (var fs = new FileStream(savePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs, ct);
            }

            return Ok(new { Id = id, PreviewUrl = (string?)null, Sucesso = true, Mensagem = "Upload realizado." });
        }

        [HttpGet("{id}")]
        public IActionResult GetMidia(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var (path, ext) = _media.ResolveFileById(id);
            if (path == null) return NotFound();

            var contentType = _media.GetContentType(ext);
            var stream = System.IO.File.OpenRead(path);
            return File(stream, contentType);
        }
    }
}