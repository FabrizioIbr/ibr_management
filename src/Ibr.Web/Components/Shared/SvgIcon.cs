using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Ibr.Web.Components.Shared;

/// <summary>
/// Acepta solo SVG de dibujo simple (como los de Tabler): etiquetas de forma, sin scripts, eventos ni enlaces.
/// Lo que no cumpla se descarta y se muestra el icono de respaldo.
/// </summary>
public static partial class SvgIcon
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "svg", "g", "path", "circle", "ellipse", "line", "polyline", "polygon", "rect", "title", "desc", "defs", "clipPath"
    };

    public static MarkupString? Sanitize(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > 20_000
            || !text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || !text.EndsWith("</svg>", StringComparison.OrdinalIgnoreCase)
            || Forbidden().IsMatch(text)
            || Tags().Matches(text).Any(tag => !AllowedTags.Contains(tag.Groups[1].Value)))
        {
            return null;
        }

        return new MarkupString(text);
    }

    [GeneratedRegex(@"<\s*/?\s*([a-zA-Z][\w:-]*)")]
    private static partial Regex Tags();

    // Atributos de evento, href/xlink:href, url(...), javascript:, entidades y bloques <! … > / <? … >.
    [GeneratedRegex(@"\son[a-z]+\s*=|href\s*=|javascript:|url\s*\(|&#|<!|<\?|style\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex Forbidden();
}
