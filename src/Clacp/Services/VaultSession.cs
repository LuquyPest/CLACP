using System;
using System.Text.Json;
using Clacp.Models;

namespace Clacp.Services;

public class VaultSession
{
    private readonly VaultService _service;
    private readonly bool _useDpapi;
    private readonly byte[]? _key;
    private readonly byte[]? _salt;
    private readonly int _iterations;

    public VaultData Data { get; }

    /// <summary>Master-password-protected session (AES-GCM, key derived via PBKDF2).</summary>
    internal VaultSession(VaultService service, byte[] key, byte[] salt, int iterations, VaultData data)
    {
        _service = service;
        _key = key;
        _salt = salt;
        _iterations = iterations;
        _useDpapi = false;
        Data = data;
    }

    /// <summary>Unprotected-mode session: encrypted at rest via Windows DPAPI, no master password required.</summary>
    internal VaultSession(VaultService service, VaultData data)
    {
        _service = service;
        _useDpapi = true;
        Data = data;
    }

    public void Save()
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(Data);

        if (_useDpapi)
        {
            var encrypted = DpapiHelper.Protect(plaintext);
            _service.WriteLocalFile(encrypted);
        }
        else
        {
            CryptoService.Encrypt(plaintext, _key!, out var nonce, out var tag, out var ciphertext);
            _service.WriteFile(_salt!, _iterations, nonce, tag, ciphertext);
        }

        Array.Clear(plaintext, 0, plaintext.Length);
    }

    public void Lock()
    {
        if (_key != null)
            Array.Clear(_key, 0, _key.Length);

        Data.Entries.Clear();
    }
}
