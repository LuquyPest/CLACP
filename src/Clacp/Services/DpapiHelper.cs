using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Clacp.Services;

/// <summary>Wraps the Windows Data Protection API (CryptProtectData/CryptUnprotectData) so data can be
/// encrypted at rest without requiring a user-supplied password - the key is tied to the current
/// Windows user account.</summary>
internal static class DpapiHelper
{
    private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, string? szDataDescr, IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DATA_BLOB pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, IntPtr ppszDataDescr, IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DATA_BLOB pDataOut);

    public static byte[] Protect(byte[] data) => Run(data, encrypt: true);

    public static byte[] Unprotect(byte[] data) => Run(data, encrypt: false);

    private static byte[] Run(byte[] data, bool encrypt)
    {
        var inBlob = new DATA_BLOB { cbData = data.Length, pbData = Marshal.AllocHGlobal(data.Length) };
        Marshal.Copy(data, 0, inBlob.pbData, data.Length);

        try
        {
            bool ok;
            DATA_BLOB outBlob;

            if (encrypt)
                ok = CryptProtectData(ref inBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, out outBlob);
            else
                ok = CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, out outBlob);

            if (!ok)
                throw new CryptographicException("Echec DPAPI.");

            var result = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
            Marshal.FreeHGlobal(outBlob.pbData);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(inBlob.pbData);
        }
    }
}
