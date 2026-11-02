using Levio.Modules.Identity.Application.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Levio.Modules.Identity.Application.Services;

internal sealed class InitialAdministratorProvisioningService(
    IServiceScopeFactory scopes,
    IOptions<InitialAdministratorSettings> options,
    ILogger<InitialAdministratorProvisioningService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured) return;

        await using var scope = scopes.CreateAsyncScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<InitialAdministratorProvisioner>();
        if (await provisioner.ProvisionAsync(settings))
            logger.LogInformation("Initial HR administrator provisioned successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
