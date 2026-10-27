# Instrucciones para asistentes de IA — Ibr.Management

Lee `README.md` (arquitectura y secretos) y, para cualquier cambio de UI, **`docs/ui-standards.md` completo**.

## Stack

.NET 10 · Blazor Server (`src/Ibr.Web`) · Radzen.Blazor · EF Core 10 + SQL Server · xUnit + SQLite en memoria.
Capas: `Domain` (entidades) → `Application` (contratos/DTOs) → `Infrastructure` (EF, servicios) → `Web`/`Api`.

## Reglas de UI (resumen; el detalle está en docs/ui-standards.md)

- Solo **Radzen**; no agregar otras librerías de componentes ni clases de Bootstrap.
- Listados: **`<SchemaDataGrid>`**. Crear/editar: **`SchemaForm.OpenAsync(...)`** (popup). Vista de referencia: `src/Ibr.Web/Components/Pages/MasterData.razor`.
- **Portales y secciones viven en la BD** (tablas `Portals`, `PortalSections`, `UserPortals`; se administran en Gestión Maestros → Portales / Secciones). No los declares en código.
- `/dashboard` = selector de portales (`HubLayout`, sin sidebar). Cada página de un portal usa `@layout PortalLayout`, que arma su **propio sidebar** y aplica el acceso (rol del portal o asignación en `UserPortals`).
- **"Crear un nuevo portal X"** = registrar el portal y sus secciones (con su `Route`) en la BD + crear el diseño de cada página en `Components/Pages/Portals/X/` con `@page` igual a esa `Route`. Receta completa: sección "Receta P" de `docs/ui-standards.md`.
- Toda página empieza con `<PageHeader>`.
- Tabla maestra nueva = entidad + configuración EF + una entrada en `src/Ibr.Infrastructure/Maestros/MaestrosCatalogRegistry.cs`. No crear páginas por tabla.
- Estilos compartidos solo en `src/Ibr.Web/wwwroot/css/ibr-theme.css` (prefijo `ibr-`, variables `--ibr-*`). Los `.razor.css` son aislados por componente.
- Textos en español.

## Antes de entregar

```powershell
dotnet build
dotnet test
```

Sin errores ni advertencias nuevas. No modificar el esquema de la base de datos sin pedirlo explícitamente.
