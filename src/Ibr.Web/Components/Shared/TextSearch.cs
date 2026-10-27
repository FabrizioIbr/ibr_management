using System.Globalization;
using System.Text;

namespace Ibr.Web.Components.Shared;

/// <summary>Búsqueda sin distinguir mayúsculas ni tildes ("posicion" encuentra "Posición").</summary>
public static class TextSearch
{
    public static bool Matches(string? term, params string?[] values) =>
        string.IsNullOrWhiteSpace(term) || Normalize(string.Join(' ', values)).Contains(Normalize(term));

    private static string Normalize(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
