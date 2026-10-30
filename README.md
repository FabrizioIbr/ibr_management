# Ibr.Management

API de gestión de usuarios basada en .NET 10, ASP.NET Core, Entity Framework Core 10 y SQL Server.

## Arquitectura

- `Ibr.Domain`: entidades de negocio puras (`User`, `Role`, `Permission` y `Position`). No depende de EF Core.
- `Ibr.Application`: contratos de casos de uso y DTOs. Las respuestas de la API no exponen entidades del dominio.
- `Ibr.Infrastructure`: persistencia SQL Server, `IbrDbContext`, configuraciones EF y servicios que implementan los contratos de Application.
- `Ibr.Api`: composición de dependencias, controladores, middleware y configuración HTTP.
- `tests/Ibr.Tests`: pruebas xUnit. La prueba del servicio de usuarios usa SQLite en memoria y no toca la base local.

Las tablas puente `UserRoles` y `RolePermissions` son relaciones many-to-many puras y se configuran con `UsingEntity` sin crear entidades CLR para ellas.

## Interfaz web

`src/Ibr.Web` (Blazor Server + Radzen) sigue el estándar de [docs/ui-standards.md](docs/ui-standards.md): grid `SchemaDataGrid`, formulario popup `SchemaForm`, `PortalLayout`. Los portales (tarjeta del Dashboard + sidebar propio) y sus secciones se definen en las tablas `Portals`, `PortalSections` y `UserPortals`, administradas desde Gestión Maestros; las tablas maestras se publican registrándolas en `src/Ibr.Infrastructure/Maestros/MaestrosCatalogRegistry.cs`. Las instrucciones para asistentes de IA están en [AGENTS.md](AGENTS.md).

## Ejecutar localmente

Requisitos: .NET SDK 10.

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Ibr.Web/Ibr.Web.csproj
```

La interfaz web (`Ibr.Web`) hospeda también los controladores de `Ibr.Api`; `Ibr.Api` ya no es un proyecto ejecutable por sí solo.

La base de datos existente no se modifica automáticamente. Las migraciones o cambios de esquema deben solicitarse explícitamente.

## Configurar secretos

La cadena de conexión no debe guardarse en `appsettings*.json` ni en el código. El proyecto web es el que se ejecuta y tiene el `UserSecretsId`; configura el secreto así:

```powershell
dotnet user-secrets --project src/Ibr.Web/Ibr.Web.csproj set "ConnectionStrings:Default" "Server=localhost;Database=ibr_management;Trusted_Connection=True;TrustServerCertificate=True"
```

Los secretos son por equipo/por usuario: al clonar en otra máquina hay que volver a ejecutar el comando con el `Server`/instancia correcta (`localhost`, `localhost\SQLEXPRESS`, `.\SQLEXPRESS`, `Server=MI-SERVIDOR;...`, etc.). Para ver o cambiar el valor actual:

```powershell
dotnet user-secrets list --project src/Ibr.Web/Ibr.Web.csproj
```

Para SQL Server con usuario y contraseña, usa el formato correspondiente de SQL Server y evita incluir credenciales en comandos compartidos o commits.

### Integración con Buk

El token de Buk tampoco debe guardarse en `appsettings*.json` ni en código. La URL base no es secreta y se define en `appsettings.json` (`https://ibrlatam.buk.pe/api/v1/peru/`). Configura el token en desarrollo con User Secrets:

```powershell
dotnet user-secrets --project src/Ibr.Web/Ibr.Web.csproj set "Buk:Token" "{token}"
```

También puedes pegar el token en `/admin/buk` para una importación puntual: se usa solo en memoria y no se guarda en la base de datos ni en el repositorio. Para no escribirlo cada vez, usa User Secrets o Key Vault.

En desarrollo puedes usar un `.env` en la raíz del repositorio (ya está ignorado por git):

```dotenv
AUTH_TOKEN={token}
BUK_BASE_URL=https://ibrlatam.buk.pe/api/v1/peru/
```

La aplicación lee `AUTH_TOKEN` como `Buk:Token` y `BUK_BASE_URL` como `Buk:BaseUrl` al iniciar. Con `Buk:AutoSync=true` (valor por defecto en `appsettings.json`) importa áreas y cargos automáticamente al arrancar. Nunca subas el `.env` ni escribas el token en logs, mensajes de error o repositorios.

El token se genera en Buk → Configuración → Accesos API.

### Azure Key Vault (producción)

En Azure App Service, crea los secretos en Key Vault y habilita Managed Identity en la app con el rol **Key Vault Secrets User** sobre el vault. Usa estos nombres (el separador `--` se traduce a `:`):

- `ConnectionStrings--Default`
- `Buk--BaseUrl`
- `Buk--Token`

Luego define en App Service la configuración **no secreta** `KeyVault__Uri` con la URL del vault (`https://mi-vault.vault.azure.net/`). Al iniciar, la app autentica con `DefaultAzureCredential` (Managed Identity) y carga los secretos; no se requiere ninguna credencial en `appsettings*.json` ni en el código.

La importación manual se dispara desde `/admin/buk` o con `POST /api/buk/sync` (requiere autenticación). Consulta `employees/active` y solo guarda empleados cuyo `current_job.company_id` sea `2` (IBR Perú); hace upsert de áreas, cargos, relaciones y empleados, y crea usuarios nuevos por correo. No toca `Companies` ni elimina registros locales.

El scaffold database-first usa `Name=ConnectionStrings:Default`, el proveedor `Microsoft.EntityFrameworkCore.SqlServer`, `IbrDbContext` y `--no-onconfiguring`.
