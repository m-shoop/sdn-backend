using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SdnBackend.Repositories;
using SdnBackend.Models;
using Npgsql;

namespace SdnBackend.Services;

public class AppointmentExpirationService : BackgroundService
{
    private readonly ILogger<AppointmentExpirationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Run every hour

    public AppointmentExpirationService(
        ILogger<AppointmentExpirationService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appointment Expiration Service is starting.");

        // Run immediately on startup, then every 12 hours
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

            // Wait 12 hours before next check
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Appointment Expiration Service is stopping.");
    }

    private async Task ExpireOldAppointments()
    {
        _logger.LogInformation("Checking for expired appointments...");

        string connectionString =
            _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string not found");

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var agreementRepo = new AgreementRepository(dataSource);

        // Get all pending appointments that have expired
        var expiredAgreements = await agreementRepo.GetExpiredPendingAgreements();

        if (expiredAgreements.Count == 0)
        {
            _logger.LogInformation("No expired appointments found.");
            return;
        }

        _logger.LogInformation($"Found {expiredAgreements.Count} expired appointment(s).");

        int expiredCount = 0;
        foreach (var agreement in expiredAgreements)
        {
            try
            {
                agreement.Expire();
                await agreementRepo.UpdateEntity(agreement);
                expiredCount++;

                _logger.LogInformation(
                    $"Expired appointment ID {agreement.Id} for client {agreement.Client.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to expire appointment ID {agreement.Id}");
            }
        }

        _logger.LogInformation($"Successfully expired {expiredCount} appointment(s).");
    }
}
