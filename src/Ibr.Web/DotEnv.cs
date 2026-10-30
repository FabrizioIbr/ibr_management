namespace Ibr.Web;

/// <summary>
/// Lector mínimo de archivos <c>.env</c> para desarrollo local. No registra ni expone valores.
/// Busca el archivo en el directorio de contenido y en sus carpetas padre.
/// </summary>
internal static class DotEnv
{
    public static IReadOnlyDictionary<string, string> Read(string contentRootPath)
    {
        var file = FindFile(contentRootPath);
        if (file is null)
        {
            return new Dictionary<string, string>();
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(file))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            values[key] = value;
        }

        return values;
    }

    private static string? FindFile(string contentRootPath)
    {
        var directory = new DirectoryInfo(contentRootPath);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
