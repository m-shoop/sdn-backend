using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SdnBackend.Repositories;

namespace SdnBackend.Services;

public class GdprCleanupService : BackgroundService
{
    private readonly ILogger<GdprCleanupService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public GdprCleanupService(ILogger<GdprCleanupService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GDPR Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during GDPR cleanup.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("GDPR Cleanup Service is stopping.");
    }

    private async Task RunCleanup()
    {
        _logger.LogInformation("Running GDPR data retention cleanup.");

        var cutoff = DateTime.UtcNow.AddYears(-1);
        var cutoffDate = DateOnly.FromDateTime(cutoff);

        using var scope = _scopeFactory.CreateScope();
        var agreementRepo = scope.ServiceProvider.GetRequiredService<AgreementRepository>();
        var accountRepo = scope.ServiceProvider.GetRequiredService<AccountRepository>();

        // Step 1: delete appointments whose date is older than one year
        int agreementsDeleted = await agreementRepo.DeleteAgreementsOlderThan(cutoffDate);
        _logger.LogInformation(
            "GDPR cleanup: deleted {Count} agreement(s) with date before {Cutoff:yyyy-MM-dd}.",
            agreementsDeleted, cutoffDate);

        // Step 2: delete client accounts with no activity in the past year
        int accountsDeleted = await accountRepo.DeleteInactiveClientAccounts(cutoff);
        _logger.LogInformation(
            "GDPR cleanup: deleted {Count} client account(s) with last activity before {Cutoff:yyyy-MM-dd}.",
            accountsDeleted, cutoffDate);
    }
}
