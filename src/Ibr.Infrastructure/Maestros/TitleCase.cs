using System.Globalization;

namespace Ibr.Infrastructure.Maestros;

/// <summary>Convierte textos importados en MAYÚSCULAS a capitalización normal para la UI.</summary>
internal static class TitleCase
{
    private static readonly HashSet<string> SmallWords =
    [
        "de", "del", "di", "el", "la", "los", "las", "un", "una", "unos", "unas",
        "y", "e", "o", "u", "a", "en", "con", "por", "para", "sin", "al", "que", "desde", "hasta"
    ];

    private static readonly HashSet<string> Acronyms =
    [
        "rrhh", "rr.ss.", "ceo", "cfo", "coo", "cto", "cio", "kpi", "sla", "bpo", "ppff.", "ddjj", "eam", "erp", "crm"
    ];

    public static string To(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;

        var words = value.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            words[i] = Word(words[i], i == 0);
        }

        return string.Join(' ', words);
    }

    private static string Word(string word, bool isFirst)
    {
        var text = word.Trim();
        if (text.Length == 0) return word;

        var lower = text.ToLowerInvariant();
        if (!isFirst && SmallWords.Contains(lower)) return lower;
        if (Acronyms.Contains(lower)) return text.ToUpperInvariant();
        if (text.Length == 2 && text.All(char.IsUpper)) return text.ToUpperInvariant();
        if (text.All(char.IsDigit)) return text;

        return Capitalize(lower);
    }

    private static string Capitalize(string lower)
    {
        var index = 0;
        while (index < lower.Length && !char.IsLetter(lower[index])) index++;
        if (index == lower.Length) return lower;

        var upper = char.ToUpper(lower[index], CultureInfo.InvariantCulture);
        return string.Concat(lower.AsSpan(0, index), upper.ToString(), lower.AsSpan(index + 1));
    }
}
