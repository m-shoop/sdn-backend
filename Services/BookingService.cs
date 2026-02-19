using SdnBackend.Dtos;
using SdnBackend.Models;
using SdnBackend.Repositories;
using Npgsql;

namespace SdnBackend.Services;

public class BookingService(NpgsqlDataSource dataSource, IEmailService emailService, ILogger<BookingService> logger)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<BookingService> _logger = logger;

    /// <summary>
    /// Processes a booking request: validates inputs, creates or retrieves client account,
    /// creates a pending agreement, and sends a confirmation email.
    /// Returns a tuple of (success, errorMessage, statusCode).
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage, int? StatusCode)> ProcessBooking(BookingRequest request)
    {
        _logger.LogInformation("Processing booking request for client {ClientEmail}", request.ClientEmail);

        // Retrieve and validate service
        Service? service = await GetService(request.Service);
        if (service is null)
            throw new InvalidOperationException("Service not found in database.");

        // Retrieve and validate tech account
        Account? techAccount = await GetAccount(request.Tech);
        if (techAccount is null)
            throw new InvalidOperationException($"Technician {request.Tech} not found in database.");

        if (techAccount is not Technician tech)
            throw new InvalidOperationException($"Account {techAccount} is not a Technician");

        // Retrieve and validate salon
        if (!int.TryParse(request.Salon, out int salonId))
            return (false, "Salon ID provided is not a valid integer", 404);

        var salonRepo = new SalonRepository(_dataSource);
        Salon? salon = await salonRepo.GetById(salonId);
        if (salon is null)
            return (false, "Salon not found in database", 404);

        // Validate email and name
        if (!IsValidEmail(request.ClientEmail))
            throw new InvalidOperationException($"Email {request.ClientEmail} is not a valid email.");

        if (request.ClientName is null)
            throw new InvalidOperationException("Name is not a valid name.");

        // Validate date and time
        if (!DateOnly.TryParseExact(request.AgreementDate, "yyyy-MM-dd", out DateOnly agreementDate))
            throw new InvalidOperationException($"Appointment date {request.AgreementDate} is not valid.");

        if (!TimeOnly.TryParseExact(request.StartTime, "HH:mm:ss", out TimeOnly startTime))
            throw new InvalidOperationException($"Appointment time {request.StartTime} is not valid.");

        // Retrieve or create client account
        var accountRepo = new AccountRepository(_dataSource);
        Account? account = await accountRepo.GetByEmailAndRole(request.ClientEmail, "Client");

        Client client;
        if (account is Client existingClient)
        {
            client = existingClient;
        }
        else
        {
            client = new Client(0, request.ClientName, request.ClientEmail);
            await accountRepo.SaveEntity(client);
        }

        // Create and persist agreement
        Agreement agreement = new Agreement
        {
            Date = agreementDate,
            Time = startTime,
            Service = service,
            Tech = tech,
            Client = client,
            Salon = salon
        };

        string confirmationToken = agreement.MarkPending();

        var agreeRepo = new AgreementRepository(_dataSource);
        await agreeRepo.SaveEntity(agreement);
        _logger.LogInformation("Agreement saved to database for client {ClientEmail}", request.ClientEmail);

        // Validate confirmation token
        if (string.IsNullOrWhiteSpace(confirmationToken))
        {
            _logger.LogError("MarkPending() returned null or empty confirmation token for client {ClientEmail}",
                request.ClientEmail);
            return (false, "Failed to generate confirmation token", 500);
        }

        _logger.LogInformation("Confirmation token generated: {TokenPreview}... for client {ClientEmail}",
            confirmationToken.Substring(0, Math.Min(8, confirmationToken.Length)),
            request.ClientEmail);

        // Send confirmation email
        ConfirmationEmail confEmail = new ConfirmationEmail
        {
            To = request.ClientEmail,
            ConfirmationToken = confirmationToken,
            AppointmentTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Attempting to send confirmation email to {ClientEmail}", request.ClientEmail);
            await _emailService.SendConfirmationAsync(confEmail, 1);
            _logger.LogInformation("Confirmation email sent successfully to {ClientEmail}", request.ClientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {ClientEmail}. Error: {ErrorMessage}",
                request.ClientEmail, ex.Message);
            return (false, $"Booking saved but failed to send confirmation email: {ex.Message}", 500);
        }

        try
        {
            bool notifyEnabled = await accountRepo.GetEmailNotificationPreference(tech.Id);
            if (notifyEnabled)
            {
                await _emailService.SendTechNotificationAsync(new TechNotificationEmail
                {
                    To = tech.Email,
                    NotificationType = "booked",
                    Date = agreementDate,
                    Time = startTime,
                    ServiceName = service.Name,
                    ServiceDuration = service.Duration
                });
                _logger.LogInformation("Tech notification sent to {TechEmail}", tech.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking notification to tech {TechId}", tech.Id);
        }

        return (true, null, null);
    }

    private async Task<Service?> GetService(string rawServiceId)
    {
        if (!int.TryParse(rawServiceId, out int serviceId))
            return null;

        var serviceRepo = new ServiceRepository(_dataSource);
        return await serviceRepo.GetById(serviceId);
    }

    private async Task<Account?> GetAccount(string rawAccountId)
    {
        if (!int.TryParse(rawAccountId, out int accountId))
            return null;

        var accountRepo = new AccountRepository(_dataSource);
        return await accountRepo.GetById(accountId);
    }

    // from StackExchange https://stackoverflow.com/questions/1365407/c-sharp-code-to-validate-email-address
    private static bool IsValidEmail(string email)
    {
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }
}
