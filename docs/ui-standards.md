# Estándar de UI — Portal iBr (Blazor + Radzen)

> **Lectura obligatoria antes de crear o modificar cualquier vista en `src/Ibr.Web`.**
> Aplica a personas y a cualquier asistente de IA (Claude, Codex, Copilot, Cursor, Gemini…).
> Si una regla no encaja con tu caso, pregunta antes de inventar un patrón nuevo.

## 1. Reglas obligatorias

1. **Una sola librería de componentes: Radzen.Blazor.** No agregues MudBlazor, Syncfusion, Telerik, Ant, Bootstrap JS, etc. No uses clases de Bootstrap (`btn`, `row`, `col-*`, `card`…) en vistas nuevas.
2. **Listados = `<SchemaDataGrid>`.** No escribas un `<RadzenDataGrid>` suelto en una página.
3. **Crear / editar = popup `SchemaForm.OpenAsync(...)`.** No crees páginas de formulario ni diálogos a mano.
4. **Los portales y sus secciones se definen en la BD, no en código** (tablas `Portals`, `PortalSections`, `UserPortals`, administradas desde Gestión Maestros → Portales / Secciones). `/dashboard` es solo el selector de portales (`@layout HubLayout`, sin sidebar). Cada página de un portal usa `@layout PortalLayout`, que arma **su propio sidebar** y controla el acceso. Toda página empieza con `<PageHeader>`.
5. **Estilos compartidos van en `wwwroot/css/ibr-theme.css`** con prefijo `ibr-`. El CSS aislado (`*.razor.css`) solo afecta a su propio componente: **nunca** pongas ahí estilos que otra página necesite (ese fue el origen del bug visual de Maestros).
6. **Colores, radios y sombras solo vía variables** (`var(--ibr-blue)`, `var(--ibr-border)`, …). Nada de hex sueltos en vistas nuevas.
7. **Tablas maestras nuevas se registran en `MaestrosCatalogRegistry`.** Menú, buscador, grid y formulario se generan solos. No crees una página por tabla.
8. **Textos de UI en español** y con el género correcto (`Feminine = true` → "Nueva posición").
9. **Antes de terminar:** `dotnet build` sin errores ni advertencias nuevas y `dotnet test` en verde.

## 2. Piezas del sistema

| Pieza | Archivo | Uso |
|---|---|---|
| Tema y tokens | `src/Ibr.Web/wwwroot/css/ibr-theme.css` | Sobrescribe variables de Radzen (`--rz-*`) con la marca y define todas las clases `ibr-*`. |
| **Portales (datos)** | Tablas `Portals`, `PortalSections`, `UserPortals` · `IPortalService` (`src/Ibr.Application/Portals/`) | **Única fuente** de tarjetas del Dashboard, inicios y sidebars. Se editan en Gestión Maestros → Portales / Secciones. |
| Layout del Dashboard | `Components/Layout/HubLayout.razor` | Barra superior con logo y usuario. Sin sidebar. Solo para `/dashboard`. |
| Layout de portal | `Components/Layout/PortalLayout.razor` | Resuelve el portal por la URL (`/portal/{PortalId}` o la `Route` de una sección y sus subrutas), arma su sidebar (con buscador si hay más de 8 secciones), **bloquea la página si el usuario no tiene acceso** o el portal no está "Disponible", y pasa el `PortalDto` actual como `[CascadingParameter]`. |
| Inicio de portal | `Components/Pages/PortalHome.razor` | Página genérica `/portal/{PortalId}`: secciones del portal como accesos, agrupadas por `SectionGroup`, con buscador si hay más de 6. |
| Icono | `Components/Shared/IconView.razor` | Pinta un icono guardado en BD: nombre Material (`work`) o código SVG (p. ej. Tabler). El SVG se valida (`SvgIcon`); si no es seguro se usa un icono de respaldo. |
| Encabezado | `Components/Shared/PageHeader.razor` | `Eyebrow`, `Title`, `Subtitle`, slot `Actions`. |
| KPI | `Components/Shared/StatCard.razor` | `Label`, `Value`, `Icon`, `Tone` (`success` / `muted`). |
| **Grid estándar** | `Components/Shared/SchemaDataGrid.razor` | Tarjeta + buscador + paginación + orden + estado vacío + acciones editar/eliminar. |
| **Formulario popup** | `Components/Shared/SchemaFormDialog.razor` + `SchemaForm.cs` | Campos generados desde el schema, validación, errores del servidor, botón con estado "Guardando…". |
| Badge de estado | `Components/Shared/StatusBadge.razor` | "Activo" → verde, "Inactivo" → gris, "Rechazado/Bloqueado" → rojo. |
| Avatar | `Components/Shared/Avatar.razor` | Iniciales del nombre. |
| Tarjeta de portal | `Components/Shared/PortalCard.razor` | Accesos del Dashboard. `Color` = hex del portal (`ColorHex`), `Icon` = Material o SVG, `Disabled` si el portal no está "Disponible". |
| Motor de catálogos | `src/Ibr.Infrastructure/Maestros/CatalogDefinition.cs` | CRUD genérico sobre EF Core; lee tipos, obligatorios, longitudes, FK y N:N de la metadata. |
| Registro de catálogos | `src/Ibr.Infrastructure/Maestros/MaestrosCatalogRegistry.cs` | **Único lugar** donde se declara cada tabla maestra. |
| Contratos | `src/Ibr.Application/Maestros/` | `CatalogSchema`, `FieldSchema`, `FieldKind`, `LookupOption`, `SaveResult`, `IMaestrosService`. |

`CatalogRow` (alias global en `src/Ibr.Web/GlobalUsings.cs`) = `IDictionary<string, object?>`: una fila del grid / valores del formulario.

## 3. Receta P — "Crear un nuevo portal X"

Los portales **no se declaran en código**: se registran en la BD desde Gestión Maestros. En código solo se crea el diseño de cada página.

**1. Registrar el portal** (Gestión Maestros → Portales → "Nuevo portal"), tabla `Portals`:

| Campo | Uso |
|---|---|
| `Name`, `Description` | Título y texto de la tarjeta del Dashboard y del inicio. |
| `ColorHex` | Color de acento (`#RRGGBB`) de la tarjeta y de la cabecera del sidebar. |
| `Icon` | Nombre de [Material Symbols](https://fonts.google.com/icons) (`shopping_cart`) o código SVG completo (p. ej. de Tabler). |
| `Status` | `Disponible` (se puede entrar) · `En construcción` / `Mantenimiento` (tarjeta bloqueada y páginas bloqueadas). |
| `RoleID` | Rol cuyos usuarios ven el portal (p. ej. `1` = System_Admin). |
| Usuarios con acceso (`UserPortals`) | Usuarios concretos que ven el portal, además del rol. |
| `IsActive` / `DeletedAt` | Inactivo o eliminado = no existe para nadie. |

Acceso: el usuario ve el portal si tiene el `RoleID` del portal **o** está en `UserPortals`. **Sin `RoleID` y sin usuarios asignados el portal es público** para cualquier usuario autenticado.

**2. Registrar sus secciones** (Gestión Maestros → Secciones), tabla `PortalSections`: `Portal`, `Title`, `Description`, `Icon`, `Route`, `SectionGroup` (caption del sidebar), `DisplayOrder`, `Status` (`Activo` = enlace; `Próximamente` = se ve deshabilitado) e `IsActive` (oculta la sección).

Con eso, automáticamente:
- aparece su **tarjeta en `/dashboard`** (solo para quien tenga acceso, con contador de secciones);
- existe su **inicio `/portal/{PortalId}`** con las secciones como accesos;
- todas las páginas cuya ruta sea la `Route` de una sección (o una subruta, p. ej. `/compras/solicitudes/15`) muestran **el sidebar de ese portal** y quedan protegidas por su acceso.

**3. Crear el diseño de cada página** en `src/Ibr.Web/Components/Pages/Portals/X/` con **la misma ruta registrada en `Route`**:

```razor
@page "/compras/solicitudes"
@layout PortalLayout
@attribute [Authorize]
```

y el contenido según la Receta B (PageHeader + SchemaDataGrid + SchemaForm). Cuando la página esté lista, cambia la sección a `Activo`.

**Reglas de portales**
- No escribas portales, tarjetas ni enlaces de sidebar en código: todo sale de `IPortalService`.
- La página toma el portal actual con `[CascadingParameter] private PortalDto? Portal { get; set; }` (breadcrumb, nombre, color).
- Una página con `PortalLayout` cuya ruta no esté registrada en ninguna sección muestra "Esta página no está registrada…": regístrala, no quites el layout.
- El acceso lo aplica `PortalLayout` (la página no se renderiza sin acceso). No dupliques los roles en `[Authorize(Roles = …)]`, porque bloquearía a los usuarios asignados por `UserPortals`.
- Si el portal necesita un inicio propio (KPIs, resumen), crea `@page "/portal/{Id}"` con el número literal (p. ej. `"/portal/7"`): tiene prioridad sobre el `PortalHome` genérico.
- Los catálogos de Gestión Maestros viven en `/admin/{slug}`; cada uno necesita su fila en Secciones para verse en el sidebar.

## 3.1 Receta A — Agregar una tabla maestra (el caso más común)

Ejemplo: tabla `Departments`.

**1. Entidad** en `src/Ibr.Domain/Entities/Department.cs` (POCO, sin atributos EF):

```csharp
public class Department
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public string? CostCenter { get; set; }
    public bool IsActive { get; set; } = true;
}
```

**2. Configuración EF** en `src/Ibr.Infrastructure/Persistence/Configurations/DepartmentConfiguration.cs` (longitudes, únicos, FK, nombres de columna) y `DbSet<Department>` en `IbrDbContext`. La BD no se migra automáticamente: los cambios de esquema se piden explícitamente (ver README).

**3. Registro** en `MaestrosCatalogRegistry.All`:

```csharp
new CatalogDefinition<Department>
{
    Slug = "departamentos", Label = "Departamentos", Singular = "departamento", Icon = "apartment",
    Group = "Organización", Description = "Áreas de la empresa y su centro de costo.",
    Display = department => department.DepartmentName,   // texto del registro en grid y listas
    StatusField = nameof(Department.IsActive)              // opcional: badge + KPIs activos/inactivos
}
    .Field(nameof(Department.DepartmentName), "Nombre", grid: false)   // ya se ve como columna principal
    .Field(nameof(Department.CostCenter), "Centro de costo")
    .Field(nameof(Department.IsActive), "Activo"),
```

**4. Sección**: en Gestión Maestros → Secciones agrega `Departamentos` con portal Gestión Maestros, ruta `/admin/departamentos`, grupo `Organización` y estado `Activo`.

Listo: `/admin/departamentos` muestra grid, KPIs y formulario, y aparece en el sidebar y en el inicio de Gestión Maestros.

**Opciones del registro**

| Miembro | Para qué |
|---|---|
| `.Field(prop, label, kind?, grid, form, options?)` | Campo escalar. El tipo se infiere de EF; `kind` solo para forzar (`FieldKind.Email`, `FieldKind.Date`, `FieldKind.LongText`). Una FK se vuelve desplegable automáticamente. Con `options: [...]` es una lista fija (`FieldKind.Choice`) y se valida al guardar. |
| `.Many(navegación, label)` | Relación N:N (skip navigation de EF) → selección múltiple con chips. |
| `.Password(label, (entidad, texto, services) => …)` | Campo virtual de contraseña: obligatorio al crear, opcional al editar. Nunca muestres el hash. |
| `StatusField` | Propiedad `bool` de activo/inactivo. |
| `SoftDeleteField` | Propiedad `DateTime?` de borrado lógico: eliminar solo la marca y el listado la excluye. |
| `TitleLabel` | Encabezado de la columna principal (por defecto "Nombre"). |
| `Feminine` | Género de `Singular` para "Nueva …" / "… creada". |
| `Group` | Agrupa en el sidebar y en el directorio. Reutiliza grupos existentes antes de crear uno. |

**Reglas del motor**

- Toda entidad referenciada por una FK o N:N **debe estar registrada** (se usa su `Display` para el texto); si no, la app lanza un error que lo indica.
- Clave primaria simple. Si es identidad se oculta; si no, se pide al crear.
- Obligatoriedad = columna no nula en EF. Longitud máxima = `HasMaxLength`. Un `string` sin longitud o mayor a 255 se edita como `LongText`.
- Los errores de BD (único duplicado, registro en uso) vuelven como `SaveResult.Fail` con un mensaje en español; el popup los muestra sin cerrarse.
- Iconos: nombres de [Material Symbols](https://fonts.google.com/icons) (`group`, `badge`, `key`, `work`, `apartment`…).

**Tipo de campo → control**

| `FieldKind` | Control Radzen |
|---|---|
| `Text`, `Email` | `RadzenTextBox` (+ validador de correo) |
| `LongText` | `RadzenTextArea` (ancho completo) |
| `Password` | `RadzenPassword` |
| `Integer`, `Decimal` | `RadzenNumeric` |
| `Boolean` | `RadzenSwitch` |
| `Date`, `DateTime` | `RadzenDatePicker` |
| `Choice` (lista fija) | `RadzenDropDown` de textos |
| `Lookup` (FK) | `RadzenDropDown` con filtro |
| `MultiLookup` (N:N) | `RadzenDropDown Multiple Chips` con filtro (ancho completo) |

Si necesitas un tipo nuevo: agrégalo a `FieldKind`, a `InferKind` (Infrastructure), a `RenderInput` (SchemaFormDialog) y a `RenderCell` (SchemaDataGrid). No lo resuelvas con un formulario aparte.

**Pruebas:** agrega casos en `tests/Ibr.Tests/MaestrosServiceTests.cs` si el catálogo tiene reglas propias (contraseña, borrado lógico, N:N). `Every_registered_catalog_builds_its_schema` ya valida que el registro sea coherente con EF.

## 4. Receta B — Nueva vista de un módulo (p. ej. Vacaciones)

Plantilla obligatoria para cada página dentro de un portal (el portal y la sección con esta `Route` ya deben estar registrados, Receta P). Copia y adapta:

```razor
@page "/vacaciones/solicitudes"
@layout PortalLayout
@attribute [Authorize]
@inject DialogService DialogService
@inject NotificationService NotificationService
@inject IVacationService VacationService   @* servicio del módulo en Application/Infrastructure *@

<PageTitle>Vacaciones</PageTitle>

<PageHeader Eyebrow="Mis portales" Title="Control de vacaciones" Subtitle="Solicita y consulta tus días.">
    <Actions>
        <RadzenButton Text="@schema.NewText" Icon="add" ButtonStyle="ButtonStyle.Primary" Click="@(() => OpenFormAsync(null))" />
    </Actions>
</PageHeader>

<div class="ibr-stats">
    <StatCard Label="Días disponibles" Value="@available.ToString()" Icon="event_available" Tone="success" />
</div>

<SchemaDataGrid Schema="@schema" Records="@rows" Loading="@loading" OnEdit="@OpenFormAsync" OnDelete="@DeleteAsync" />

@code {
    // Si el módulo no es un catálogo maestro, describe sus campos a mano con el mismo contrato.
    private static readonly CatalogSchema schema = new(
        Slug: "vacaciones", Label: "Solicitudes", Singular: "solicitud", Icon: "event_available", Group: "Vacaciones",
        Description: "Tus solicitudes de vacaciones.", KeyField: "RequestId", TitleLabel: "Motivo", StatusField: null,
        Fields:
        [
            new("RequestId", "Código", FieldKind.Integer, typeof(int), IsKey: true, ShowInGrid: false, ShowInForm: false),
            new("StartDate", "Desde", FieldKind.Date, typeof(DateTime), Required: true),
            new("EndDate", "Hasta", FieldKind.Date, typeof(DateTime), Required: true),
            new("Comment", "Comentario", FieldKind.LongText, typeof(string), MaxLength: 500),
        ],
        Feminine: true);

    // Cada fila debe traer KeyField, CatalogSchema.TitleKey ("__title") y cada campo del schema.
    private IReadOnlyList<CatalogRow> rows = [];
    // ... cargar rows, OpenFormAsync con SchemaForm.OpenAsync(...), DeleteAsync con DialogService.Confirm(...)
}
```

Copia `OpenFormAsync` y `DeleteAsync` de `Components/Pages/MasterData.razor`, que es la **vista de referencia**: confirmación al eliminar, notificación al guardar y recarga del listado.

Vistas que no son CRUD (dashboards, reportes): `PortalLayout` + `PageHeader` + `ibr-card` / `StatCard` / `PortalCard`. Si muestran una tabla, usa igualmente `SchemaDataGrid`.

## 5. Clases y tokens disponibles

- Contenedores: `ibr-card`, `ibr-card--flush`, `ibr-card__toolbar`, `ibr-card-grid` (tarjetas responsive), `ibr-stats` (fila de KPIs), `ibr-tiles` / `ibr-tile` (accesos compactos).
- Texto: `ibr-section-title`, `ibr-text-muted`, `ibr-breadcrumb`.
- Estados: `ibr-empty`, `ibr-loading`, `ibr-badge--success|neutral|danger`.
- Variables: `--ibr-blue`, `--ibr-blue-hover`, `--ibr-blue-soft`, `--ibr-pink`, `--ibr-bg`, `--ibr-surface`, `--ibr-border`, `--ibr-text`, `--ibr-text-muted`, `--ibr-text-subtle`, `--ibr-radius`, `--ibr-radius-sm`, `--ibr-shadow`.
- Botones: `RadzenButton` con `ButtonStyle.Primary` (acción principal, una por pantalla), `ButtonStyle.Base` + `Variant.Outlined` (secundaria), `Variant.Text` (acciones de fila).
- Notificaciones: `NotificationService.Notify(...)`. Confirmaciones: `DialogService.Confirm(...)` con `CssClass = "ibr-dialog"`.

## 6. Antipatrones (rechazar en revisión)

- `RadzenTabs` u otros componentes usados como menú lateral.
- Sidebar en `/dashboard`, enlaces escritos a mano en un sidebar, o portales/tarjetas escritos en código (todo sale de las tablas `Portals` / `PortalSections`).
- Páginas de un portal cuya ruta no coincide con la `Route` de una sección registrada (quedan bloqueadas y sin sidebar).
- Pintar iconos de BD con `RadzenIcon` o `MarkupString` directo: usa `<IconView>`.
- Estilos de una página en el `.razor.css` de otra.
- `!important` para pelear con Radzen: cambia la variable `--rz-*` en `ibr-theme.css`.
- Formularios inline en la página o un `.razor` de formulario por tabla.
- Consultas EF desde componentes Razor (siempre vía servicios de Application).
- Botones sin acción visibles como "habilitados": usa `Disabled` o `PortalCard Disabled="true"`.
