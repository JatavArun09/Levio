using Microsoft.Extensions.DependencyInjection;

namespace Levio.Modules.Auditing;

public static class AuditingModule
{
    public static IServiceCollection AddAuditingModule(this IServiceCollection services)
    {
        // Domain only today. Register future audit application services here.
        return services;
    }
}
