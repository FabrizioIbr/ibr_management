using System.Globalization;

namespace Ibr.Web.Components.Portals;

/// <summary>Etiquetas y clases de presentación para el semáforo del portal de vacaciones.</summary>
public static class VacationUi
{
    public const string ColorNormal = "#16A34A";
    public const string ColorAlerta = "#EAB308";
    public const string ColorCritico = "#F97316";
    public const string ColorVencido = "#DC2626";

    public static (string Label, string BadgeClass) Risk(string level) => level switch
    {
        "vencido" => ("Vencido", "danger"),
        "critico" => ("Crítico", "orange"),
        "alerta" => ("Alerta", "warning"),
        _ => ("Normal", "success")
    };

    public static string Days(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Date(DateOnly? value) => value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—";
}
