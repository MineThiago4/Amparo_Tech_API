using System.Collections.Generic;

namespace Amparo_Tech_API.Services
{
    public interface IMediaPolicyService
    {
        // Lista branca de extensões permitidas (com ponto, minúsculas)
        IReadOnlySet<string> AllowedExtensions { get; }

        // Limite máximo de tamanho por arquivo (bytes)
        long MaxBytes { get; }

        // Limites de quantidade por tipo
        int MaxImages { get; }
        int MaxVideos { get; }

        // Retorna o diretório raiz de armazenamento de uploads
        string GetStorageRoot();

        // Resolve arquivo por id (id + ".*") no storage
        (string? path, string ext) ResolveFileById(string id);

        // Classificadores auxiliares
        bool IsVideoExt(string ext);

        // Obtém o Content-Type correspondente à extensão
        string GetContentType(string ext);

        // Valida a lista de ids (existência, extensão permitida, tamanho) e retorna contagens por tipo
        void ValidateIdsOrThrow(IEnumerable<string> ids, out int imagens, out int videos);
    }
}
