using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using SdnBackend.Repositories;

namespace SdnBackend.Services;

public class GdprCleanupService : BackgroundService
{
    private readonly ILogger<GdprCleanupService> _logger;
    private readonly NpgsqlDataSource _dataSource;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    public GdprCleanupService(ILogger<GdprCleanupService> logger, NpgsqlDataSource dataSource)
    {
        _logger = logger;
        _dataSource = dataSource;
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

        var agreementRepo = new AgreementRepository(_dataSource);
        var accountRepo = new AccountRepository(_dataSource);

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
