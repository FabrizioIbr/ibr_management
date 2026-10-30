using Ibr.Application.Auth;
using Ibr.Application.Buk;
using Ibr.Application.Maestros;
using Ibr.Application.Portals;
using Ibr.Application.Users;
using Ibr.Application.Vacations;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Auth;
using Ibr.Infrastructure.Buk;
using Ibr.Infrastructure.Persistence;
using Ibr.Infrastructure.Portals;
using Ibr.Infrastructure.Users;
using Ibr.Infrastructure.Vacations;
using Ibr.Infrastructure.Maestros;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ibr.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? contentRootPath = null)
    {
        // La fábrica da DbContexts independientes (PortalService); el DbContext scoped sigue disponible para el resto.
        services.AddDbContextFactory<IbrDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));
        services.AddScoped(provider => provider.GetRequiredService<IDbContextFactory<IbrDbContext>>().CreateDbContext());

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMaestrosService, MaestrosService>();
        services.AddScoped<IPortalService, PortalService>();

        services.Configure<BukOptions>(configuration.GetSection(BukOptions.SectionName));
        services.AddHttpClient(nameof(BukApiClient), client =>
        {
            // El endpoint employees puede tardar más de un minuto en responder y devuelve datos voluminosos.
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        services.AddScoped<IBukApiClient, BukApiClient>();
        services.AddScoped<IBukSyncService, BukSyncService>();
        services.AddScoped<IVacationSyncService, VacationSyncService>();
        services.AddScoped<IVacationAdminService, VacationAdminService>();
        services.AddSingleton<IVacationSettingsService>(new VacationSettingsService(contentRootPath));
        services.AddScoped<IVacationTeamService, VacationTeamService>();
        return services;
    }
}
