using Ibr.Application.Maestros;
using Radzen;

namespace Ibr.Web.Components.Shared;

/// <summary>Punto de entrada único para abrir el formulario popup estándar.</summary>
public static class SchemaForm
{
    /// <summary>
    /// Abre <see cref="SchemaFormDialog"/>. <paramref name="record"/> null = crear.
    /// Devuelve true si se guardó. El diálogo permanece abierto y muestra el error si <paramref name="save"/> falla.
    /// </summary>
    public static async Task<bool> OpenAsync(
        DialogService dialogs,
        CatalogSchema schema,
        CatalogRow? record,
        IReadOnlyDictionary<string, IReadOnlyList<LookupOption>> lookups,
        Func<CatalogRow, Task<SaveResult>> save)
    {
        var isNew = record is null;
        var values = isNew ? NewValues(schema) : new Dictionary<string, object?>(record!);

        var result = await dialogs.OpenAsync<SchemaFormDialog>(
            isNew ? schema.NewText : $"Editar {schema.Singular}",
            new Dictionary<string, object?>
            {
                [nameof(SchemaFormDialog.Schema)] = schema,
                [nameof(SchemaFormDialog.Values)] = values,
                [nameof(SchemaFormDialog.Lookups)] = lookups,
                [nameof(SchemaFormDialog.IsNew)] = isNew,
                [nameof(SchemaFormDialog.Save)] = save
            },
            new DialogOptions { Width = "680px", CssClass = "ibr-dialog", CloseDialogOnOverlayClick = false, CloseDialogOnEsc = true });

        return result is true;
    }

    private static Dictionary<string, object?> NewValues(CatalogSchema schema) =>
        schema.Fields.ToDictionary(
            field => field.Name,
            field => field.Kind switch
            {
                FieldKind.Boolean => field.Name == schema.StatusField,
                FieldKind.MultiLookup => new List<object>(),
                _ => (object?)null
            });
}
