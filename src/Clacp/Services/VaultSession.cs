using System;
using System.Text.Json;
using Clacp.Models;

namespace Clacp.Services;

public class VaultSession
{
    private readonly VaultService _service;
    private readonly byte[] _key;
    private readonly byte[] _salt;
    private readonly int _iterations;

    public VaultData Data { get; }

    internal VaultSession(VaultService service, byte[] key, byte[] salt, int iterations, VaultData data)
    {
        _service = service;
        _key = key;
        _salt = salt;
        _iterations = iterations;
        Data = data;
    }

    public void Save()
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(Data);
        CryptoService.Encrypt(plaintext, _key, out var nonce, out var tag, out var ciphertext);
        _service.WriteFile(_salt, _iterations, nonce, tag, ciphertext);
        Array.Clear(plaintext, 0, plaintext.Length);
    }

    public void Lock()
    {
        Array.Clear(_key, 0, _key.Length);
        Data.Entries.Clear();
    }
}
