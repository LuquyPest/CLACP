using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clacp.Models;

namespace Clacp.Services;

public class VaultService
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("CLCP1");

    public string VaultPath { get; }

    public VaultService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clacp");
        Directory.CreateDirectory(dir);
        VaultPath = Path.Combine(dir, "vault.dat");
    }

    public bool VaultExists() => File.Exists(VaultPath);

    public VaultSession CreateVault(string masterPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(CryptoService.SaltSize);
        var key = CryptoService.DeriveKey(masterPassword, salt, CryptoService.DefaultIterations);
        var session = new VaultSession(this, key, salt, CryptoService.DefaultIterations, new VaultData());
        session.Save();
        return session;
    }

    public VaultSession? Unlock(string masterPassword)
    {
        var raw = File.ReadAllBytes(VaultPath);
        using var stream = new MemoryStream(raw);
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadBytes(Magic.Length);
        if (!magic.AsSpan().SequenceEqual(Magic))
            throw new InvalidDataException("Fichier de coffre invalide.");

        var salt = reader.ReadBytes(CryptoService.SaltSize);
        var iterations = reader.ReadInt32();
        var nonce = reader.ReadBytes(CryptoService.NonceSize);
        var tag = reader.ReadBytes(CryptoService.TagSize);
        var ciphertext = reader.ReadBytes((int)(stream.Length - stream.Position));

        var key = CryptoService.DeriveKey(masterPassword, salt, iterations);

        byte[] plaintext;
        try
        {
            plaintext = CryptoService.Decrypt(ciphertext, key, nonce, tag);
        }
        catch (CryptographicException)
        {
            return null;
        }

        var data = JsonSerializer.Deserialize<VaultData>(plaintext) ?? new VaultData();
        return new VaultSession(this, key, salt, iterations, data);
    }

    internal void WriteFile(byte[] salt, int iterations, byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        var tempPath = VaultPath + ".tmp";
        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(Magic);
            writer.Write(salt);
            writer.Write(iterations);
            writer.Write(nonce);
            writer.Write(tag);
            writer.Write(ciphertext);
        }

        File.Copy(tempPath, VaultPath, overwrite: true);
        File.Delete(tempPath);
    }
}
