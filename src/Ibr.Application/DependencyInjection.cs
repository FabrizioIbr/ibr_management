using Microsoft.Extensions.DependencyInjection;

namespace Ibr.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application-only validators and services here.
        return services;
    }
}
