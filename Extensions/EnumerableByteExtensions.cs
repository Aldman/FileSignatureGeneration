using System.Security.Cryptography;
using System.Text;

namespace FileSignatureGeneration.Extensions;

public static class EnumerableByteExtensions
{
    public static string ToHexString (this byte[] bytes)
    {
        var sb = new StringBuilder();
        var hash = SHA256.HashData(bytes);
        
        foreach (var sigh in hash)
        {
            sb.Append($"{sigh:X2}");
        }

        return sb.ToString();
    }
}