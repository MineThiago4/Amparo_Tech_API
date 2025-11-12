using System.Security.Cryptography;
using System.Text;

namespace Amparo_Tech_API.Services
{
    public interface IMessageCryptoService
    {
        string Encrypt(string plaintext);
        string Decrypt(string ciphertextBase64);
    }

    public class AesGcmMessageCryptoService : IMessageCryptoService
    {
        private readonly byte[] _key;
        private const int NonceSize = 12; // recomendado para GCM
        private const int TagSize = 16;   // 128-bit tag
        public AesGcmMessageCryptoService(IConfiguration cfg)
        {
            // Tenta ler chave base64; se ausente, deriva de Jwt:Key para ambiente dev
            var base64 = cfg["Messages:Key"];
            if (!string.IsNullOrWhiteSpace(base64))
            {
                try { _key = Convert.FromBase64String(base64); }
                catch { _key = DeriveFromJwt(cfg); }
            }
            else
            {
                _key = DeriveFromJwt(cfg);
            }
            if (_key.Length is not (16 or 24 or 32))
            {
                // Ajusta para 32 bytes via SHA256
                using var sha = SHA256.Create();
                _key = sha.ComputeHash(_key);
            }
        }

        private static byte[] DeriveFromJwt(IConfiguration cfg)
        {
            var jwt = cfg["Jwt:Key"] ?? "fallback-dev-key";
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(jwt));
        }

        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext ?? string.Empty;
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var pt = Encoding.UTF8.GetBytes(plaintext);
            var ct = new byte[pt.Length];
            var tag = new byte[TagSize];
            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, pt, ct, tag);
            var output = new byte[1 + nonce.Length + tag.Length + ct.Length];
            output[0] = 1; // versão
            Buffer.BlockCopy(nonce, 0, output, 1, nonce.Length);
            Buffer.BlockCopy(tag, 0, output, 1 + nonce.Length, tag.Length);
            Buffer.BlockCopy(ct, 0, output, 1 + nonce.Length + tag.Length, ct.Length);
            return Convert.ToBase64String(output);
        }

        public string Decrypt(string ciphertextBase64)
        {
            if (string.IsNullOrWhiteSpace(ciphertextBase64)) return string.Empty;
            var data = Convert.FromBase64String(ciphertextBase64);
            var version = data[0];
            if (version != 1) throw new CryptographicException("Versão de criptografia inválida");
            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var ct = new byte[data.Length - 1 - nonce.Length - tag.Length];
            Buffer.BlockCopy(data, 1, nonce, 0, nonce.Length);
            Buffer.BlockCopy(data, 1 + nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(data, 1 + nonce.Length + tag.Length, ct, 0, ct.Length);
            var pt = new byte[ct.Length];
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, ct, tag, pt);
            return Encoding.UTF8.GetString(pt);
        }
    }
}
