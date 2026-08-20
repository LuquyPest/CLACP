using System.Security.Cryptography;
using System.Text;

namespace Clacp.Services;

public static class PasswordGenerator
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.?";

    public static string Generate(int length = 20, bool includeSymbols = true)
    {
        var alphabet = Lower + Upper + Digits + (includeSymbols ? Symbols : string.Empty);
        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(alphabet.Length);
            result.Append(alphabet[index]);
        }

        return result.ToString();
    }
}
