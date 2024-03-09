using System.Text;

namespace FileSignatureGeneration.Extensions;

public static class EnumerableByteExtensions
{
    public static string ToHexString<T>(this IEnumerable<T> bytes)
    {
        var sb = new StringBuilder();
        foreach (var sigh in bytes)
        {
            sb.Append($"{sigh:X2}");
        }

        return sb.ToString();
    }
}