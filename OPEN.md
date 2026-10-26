# Ibr.Management

## Stack
- .NET 10, ASP.NET Core Web API (controllers), EF Core 10, SQL Server
- Base local: `ibr_management` (database-first, ya existe)
- Pruebas: xUnit. Logs: Serilog

## Arquitectura (dependencias hacia adentro)
- Ibr.Domain: entidades puras, sin paquetes de infraestructura
- Ibr.Application: casos de uso, DTOs, interfaces, FluentValidation
- Ibr.Infrastructure: DbContext, configuraciones EF, repositorios
- Ibr.Api: controllers, DI, middleware, configuración
- Prohibido: MediatR, AutoMapper. Mapeo manual con métodos de extensión.

## Reglas
- Nunca exponer entidades en la API; solo DTOs
- async/await con CancellationToken en todo acceso a datos
- Prohibido escribir secretos en el código o en appsettings*.json:
  la cadena de conexión va en User Secrets (clave `ConnectionStrings:Default`)
- Fechas siempre en UTC
- Borrado lógico: Users.DeletedAt NULL = activo
- Cambios pequeños, un commit por tarea, explicar qué hiciste al terminar
- No tocar la base de datos sin que yo lo pida