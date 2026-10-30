namespace Ibr.Web.Components.Shared;

using System.Globalization;
using System.Text;

/// <summary>Normalización de textos de catálogos para la UI (guiones bajos, estados en español).</summary>
internal static class CatalogUiText
{
    /// <summary>Texto legible: los guiones bajos son de la base de datos, no de la interfaz.</summary>
    public static string? Display(string? value) => value?.Replace('_', ' ');

    /// <summary>"active"/"inactive"/"activo"/"inactivo" → etiqueta en español con inicial mayúscula.</summary>
    public static bool TryStatusLabel(string? raw, out string label)
    {
        switch (raw?.Trim().ToLowerInvariant())
        {
            case "active" or "activo":
                label = "Activo";
                return true;
            case "inactive" or "inactivo":
                label = "Inactivo";
                return true;
            default:
                label = string.Empty;
                return false;
        }
    }

    /// <summary>Texto de celda filtrable: etiqueta de estado si aplica, si no texto normalizado.</summary>
    public static string CellLabel(string? rawValue)
        => TryStatusLabel(rawValue, out var status) ? status : Display(rawValue) ?? string.Empty;

    /// <summary>Clave de comparación: minúsculas, sin tildes ni símbolos (para casar grupos configurados).</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
