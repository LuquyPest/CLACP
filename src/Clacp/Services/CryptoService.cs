using System.Security.Cryptography;

namespace Clacp.Services;

public static class CryptoService
{
    public const int SaltSize = 16;
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int KeySize = 32;
    public const int DefaultIterations = 600_000;

    public static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);
    }

    public static void Encrypt(byte[] plaintext, byte[] key, out byte[] nonce, out byte[] tag, out byte[] ciphertext)
    {
        nonce = RandomNumberGenerator.GetBytes(NonceSize);
        tag = new byte[TagSize];
        ciphertext = new byte[plaintext.Length];
        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] nonce, byte[] tag)
    {
        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
