using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SdnBackend.Repositories;
using SdnBackend.Models;

namespace SdnBackend.Services;

public class AppointmentExpirationService : BackgroundService
{
    private readonly ILogger<AppointmentExpirationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Run every hour

    public AppointmentExpirationService(
        ILogger<AppointmentExpirationService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appointment Expiration Service is starting.");

        // Run immediately on startup, then every hour
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireOldAppointments();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while expiring appointments.");
            }

            // Wait before next check
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Appointment Expiration Service is stopping.");
    }

    private async Task ExpireOldAppointments()
    {
        _logger.LogInformation("Checking for expired appointments...");

        using var scope = _scopeFactory.CreateScope();
        var agreementRepo = scope.ServiceProvider.GetRequiredService<AgreementRepository>();

        // Get all pending appointments that have expired
        var expiredAgreements = await agreementRepo.GetExpiredPendingAgreements();

        if (expiredAgreements.Count == 0)
        {
            _logger.LogInformation("No expired appointments found.");
            return;
        }

        _logger.LogInformation("Found {Count} expired appointment(s).", expiredAgreements.Count);

        int expiredCount = 0;
        foreach (var agreement in expiredAgreements)
        {
            try
            {
                agreement.Expire();
                await agreementRepo.UpdateEntity(agreement);
                expiredCount++;

                _logger.LogInformation(
                    "Expired appointment ID {Id} for client {Email}.",
                    agreement.Id, agreement.Client.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire appointment ID {Id}.", agreement.Id);
            }
        }

        _logger.LogInformation("Successfully expired {Count} appointment(s).", expiredCount);
    }
}
