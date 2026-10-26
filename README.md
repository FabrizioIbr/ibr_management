# Ibr.Management

API de gestión de usuarios basada en .NET 10, ASP.NET Core, Entity Framework Core 10 y SQL Server.

## Arquitectura

- `Ibr.Domain`: entidades de negocio puras (`User`, `Role`, `Permission` y `Position`). No depende de EF Core.
- `Ibr.Application`: contratos de casos de uso y DTOs. Las respuestas de la API no exponen entidades del dominio.
- `Ibr.Infrastructure`: persistencia SQL Server, `IbrDbContext`, configuraciones EF y servicios que implementan los contratos de Application.
- `Ibr.Api`: composición de dependencias, controladores, middleware y configuración HTTP.
- `tests/Ibr.Tests`: pruebas xUnit. La prueba del servicio de usuarios usa SQLite en memoria y no toca la base local.

Las tablas puente `UserRoles` y `RolePermissions` son relaciones many-to-many puras y se configuran con `UsingEntity` sin crear entidades CLR para ellas.

## Ejecutar localmente

Requisitos: .NET SDK 10.

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Ibr.Api/Ibr.Api.csproj
```

La base de datos existente no se modifica automáticamente. Las migraciones o cambios de esquema deben solicitarse explícitamente.

## Configurar secretos

La cadena de conexión no debe guardarse en `appsettings*.json` ni en el código. El proyecto API tiene un `UserSecretsId`; configura el secreto así:

```powershell
dotnet user-secrets --project src/Ibr.Api/Ibr.Api.csproj set "ConnectionStrings:Default" "Server=localhost;Database=ibr_management;Trusted_Connection=True;TrustServerCertificate=True"
```

Para SQL Server con usuario y contraseña, usa el formato correspondiente de SQL Server y evita incluir credenciales en comandos compartidos o commits.

El scaffold database-first usa `Name=ConnectionStrings:Default`, el proveedor `Microsoft.EntityFrameworkCore.SqlServer`, `IbrDbContext` y `--no-onconfiguring`.
