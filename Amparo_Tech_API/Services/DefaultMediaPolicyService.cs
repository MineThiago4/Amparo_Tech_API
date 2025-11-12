using System.Collections.Generic;

namespace Amparo_Tech_API.Services
{
    public class DefaultMediaPolicyService : IMediaPolicyService
    {
        private static readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".gif", ".mp4", ".mov"
        };

        public IReadOnlySet<string> AllowedExtensions => _allowed;
        public long MaxBytes => 50L * 1024 * 1024; // 50MB
        public int MaxImages => 5;
        public int MaxVideos => 2;

        public string GetStorageRoot()
        {
            var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
            Directory.CreateDirectory(root);
            return root;
        }

        public (string? path, string ext) ResolveFileById(string id)
        {
            var root = GetStorageRoot();
            var file = Directory.EnumerateFiles(root, id + ".*").FirstOrDefault();
            if (file == null) return (null, "");
            return (file, Path.GetExtension(file).ToLowerInvariant());
        }

        public bool IsVideoExt(string ext) => ext is ".mp4" or ".mov";

        public string GetContentType(string ext) => ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" or ".heif" => "image/heic",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };

        public void ValidateIdsOrThrow(IEnumerable<string> ids, out int imagens, out int videos)
        {
            imagens = 0; videos = 0;
            foreach (var id in ids)
            {
                var (path, ext) = ResolveFileById(id);
                if (path == null) throw new InvalidOperationException($"Mídia '{id}' não encontrada.");
                if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
                    throw new InvalidOperationException($"Tipo de arquivo não permitido: '{ext}'.");

                try
                {
                    var fi = new FileInfo(path);
                    if (fi.Length > MaxBytes)
                        throw new InvalidOperationException($"Arquivo excede o limite de 50MB: '{id}'.");
                }
                catch (Exception)
                {
                    throw new InvalidOperationException($"Não foi possível validar a mídia '{id}'.");
                }

                if (IsVideoExt(ext)) videos++; else imagens++;
            }

            if (imagens > MaxImages) throw new InvalidOperationException($"Máximo de {MaxImages} imagens excedido.");
            if (videos > MaxVideos) throw new InvalidOperationException($"Máximo de {MaxVideos} vídeos excedido.");
        }
    }
}
