using SdnBackend.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Reflection.Metadata.Ecma335;
using System.Drawing;
using Microsoft.Extensions.Logging;


namespace SdnBackend.Services;

public class ConfirmationEmail
{
    public required string To { get; set; }
    public string? ConfirmationToken { get; set; }
    public DateTime AppointmentTime { get; set; }
}

public class TechNotificationEmail
{
    public required string To { get; set; }
    /// <summary>"booked" | "modified" | "cancelled"</summary>
    public required string NotificationType { get; set; }
    public required DateOnly Date { get; set; }
    public required TimeOnly Time { get; set; }
    public required string ServiceName { get; set; }
    public required int ServiceDuration { get; set; }
    // Only set for "modified" notifications
    public DateOnly? OldDate { get; set; }
    public TimeOnly? OldTime { get; set; }
    public string? OldServiceName { get; set; }
    public int? OldServiceDuration { get; set; }
}

public class ClientNotificationEmail
{
    public required string To { get; set; }
    public required string ClientName { get; set; }
    /// <summary>"booked" | "modified" | "cancelled"</summary>
    public required string NotificationType { get; set; }
    public required DateOnly Date { get; set; }
    public required TimeOnly Time { get; set; }
    public required string ServiceName { get; set; }
    public required int ServiceDuration { get; set; }
    // Only set for "modified" notifications
    public DateOnly? OldDate { get; set; }
    public TimeOnly? OldTime { get; set; }
    public string? OldServiceName { get; set; }
    public int? OldServiceDuration { get; set; }
}

public interface IEmailService
{
    public Task SendConfirmationAsync(ConfirmationEmail email, int emailTemplate);
    public Task SendPasswordResetAsync(string toEmail, string resetToken);
    public Task SendTechNotificationAsync(TechNotificationEmail email);
    public Task SendClientNotificationAsync(ClientNotificationEmail email);
}

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(SmtpSettings settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }   

    // emailTemplate: 1 for confirmation link, 2 (default) for final confirmation
    public async Task SendConfirmationAsync(ConfirmationEmail email, int emailTemplate)
    {
        try
        {
            _logger.LogInformation("Starting email send process to {EmailAddress} with template {Template}", email.To, emailTemplate);

            // Validate email parameters
            if (string.IsNullOrWhiteSpace(email.To))
            {
                _logger.LogError("Email address is null or empty");
                throw new ArgumentException("Email address cannot be null or empty", nameof(email));
            }

            if (emailTemplate == 1 && string.IsNullOrWhiteSpace(email.ConfirmationToken))
            {
                _logger.LogError("Confirmation token is null or empty for email template 1");
                throw new ArgumentException("Confirmation token is required for email template 1", nameof(email));
            }

            // MailKit SMTP logic here
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _settings.FromName,
                _settings.FromEmail   // MUST be verified in Sweego
            ));
            message.To.Add(MailboxAddress.Parse(email.To));
            message.Subject = "Appointment Confirmation at Shooper Dooper";

            var builder = new BodyBuilder();
            // Set the plain-text version first
            // TODO : add baseURL to app settings file, for now hardcoded
            if (emailTemplate == 1)
            {
                builder.TextBody = TextConfirmationLinkBody(email.ConfirmationToken!);
                builder.HtmlBody = HtmlConfirmationLinkBody(email.ConfirmationToken!);
                _logger.LogInformation("Built confirmation link email body");
            }
            else //email Template == 2, default
            {
                builder.TextBody = TextFinalConfirmationBody(email.AppointmentTime);
                builder.HtmlBody = HtmlFinalConfirmationBody(email.AppointmentTime);
                _logger.LogInformation("Built final confirmation email body");
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            // Temporarily keep this to isolate the issue
            client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true; // Accept for now to see what the actual error is

            _logger.LogInformation("Attempting to connect to SMTP server {Host}:{Port}", _settings.Host, _settings.Port);

            // Try Auto first
            await client.ConnectAsync(
                _settings.Host,   // your SMTP host
                _settings.Port,
                SecureSocketOptions.Auto
            );

            _logger.LogInformation("Successfully connected to SMTP server");

            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password
            );

            _logger.LogInformation("Successfully authenticated with SMTP server");

            await client.SendAsync(message);

            _logger.LogInformation("Email sent successfully to {EmailAddress}", email.To);

            await client.DisconnectAsync(true);

            _logger.LogInformation("Disconnected from SMTP server");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Validation error while preparing email to {EmailAddress}", email.To);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {EmailAddress}. Error: {ErrorMessage}", email.To, ex.Message);
            throw new InvalidOperationException($"Failed to send confirmation email to {email.To}", ex);
        }
    }

    public async Task SendTechNotificationAsync(TechNotificationEmail email)
    {
        try
        {
            _logger.LogInformation("Sending tech notification ({Type}) to {Email}", email.NotificationType, email.To);

            var (subject, textBody, htmlBody) = email.NotificationType switch
            {
                "booked" => (
                    "New appointment scheduled",
                    TextTechNotificationBody(email, "A new appointment has been added to your schedule."),
                    HtmlTechNotificationBody(email, "New Appointment", "A new appointment has been added to your schedule.")
                ),
                "modified" => (
                    "Appointment updated",
                    TextTechNotificationBody(email, "An appointment on your schedule has been updated."),
                    HtmlTechNotificationBody(email, "Appointment Updated", "An appointment on your schedule has been updated.")
                ),
                "cancelled" => (
                    "Appointment cancelled",
                    TextTechNotificationBody(email, "An appointment on your schedule has been cancelled."),
                    HtmlTechNotificationBody(email, "Appointment Cancelled", "An appointment on your schedule has been cancelled.")
                ),
                _ => throw new ArgumentException($"Unknown notification type: {email.NotificationType}")
            };

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(email.To));
            message.Subject = subject;

            var builder = new BodyBuilder { TextBody = textBody, HtmlBody = htmlBody };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Tech notification ({Type}) sent to {Email}", email.NotificationType, email.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send tech notification to {Email}", email.To);
            throw;
        }
    }

    public async Task SendClientNotificationAsync(ClientNotificationEmail email)
    {
        try
        {
            _logger.LogInformation("Sending client notification ({Type}) to {Email}", email.NotificationType, email.To);

            var (subject, textBody, htmlBody) = email.NotificationType switch
            {
                "booked" => (
                    "Your appointment is confirmed — Shooper Dooper Nails",
                    TextClientNotificationBody(email, $"Hi {email.ClientName}, your appointment at Shooper Dooper Nails has been confirmed."),
                    HtmlClientNotificationBody(email, "Appointment Confirmed", $"Hi {email.ClientName}, your appointment has been confirmed.")
                ),
                "modified" => (
                    "Your appointment has been updated — Shooper Dooper Nails",
                    TextClientNotificationBody(email, $"Hi {email.ClientName}, your appointment at Shooper Dooper Nails has been updated."),
                    HtmlClientNotificationBody(email, "Appointment Updated", $"Hi {email.ClientName}, your appointment has been updated.")
                ),
                "cancelled" => (
                    "Your appointment has been cancelled — Shooper Dooper Nails",
                    TextClientNotificationBody(email, $"Hi {email.ClientName}, your appointment at Shooper Dooper Nails has been cancelled."),
                    HtmlClientNotificationBody(email, "Appointment Cancelled", $"Hi {email.ClientName}, your appointment has been cancelled.")
                ),
                _ => throw new ArgumentException($"Unknown notification type: {email.NotificationType}")
            };

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(email.To));
            message.Subject = subject;

            var builder = new BodyBuilder { TextBody = textBody, HtmlBody = htmlBody };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Client notification ({Type}) sent to {Email}", email.NotificationType, email.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send client notification to {Email}", email.To);
            throw;
        }
    }

    private static string TextClientNotificationBody(ClientNotificationEmail email, string headline)
    {
        if (email.OldDate is not null)
        {
            return @$"{headline}

Previous:
  Date:    {email.OldDate:dddd, MMMM d, yyyy}
  Time:    {email.OldTime:h:mm tt}
  Service: {email.OldServiceName} ({email.OldServiceDuration} min)

Updated to:
  Date:    {email.Date:dddd, MMMM d, yyyy}
  Time:    {email.Time:h:mm tt}
  Service: {email.ServiceName} ({email.ServiceDuration} min)

— Shooper Dooper Nails";
        }

        return @$"{headline}

Date:    {email.Date:dddd, MMMM d, yyyy}
Time:    {email.Time:h:mm tt}
Service: {email.ServiceName} ({email.ServiceDuration} min)

— Shooper Dooper Nails";
    }

    private static string HtmlClientNotificationBody(ClientNotificationEmail email, string heading, string intro)
    {
        var appointmentRows = email.OldDate is not null
            ? $@"<tr>
                <td colspan=""2"" style=""font-size: 11px; font-weight: bold; text-transform: uppercase; letter-spacing: 0.05em; color: #999; padding: 8px 8px 4px 8px;"">Previous</td>
              </tr>
              <tr style=""color: #999;"">
                <td style=""font-weight: bold; width: 90px; padding: 4px 8px;"">Date</td>
                <td style=""padding: 4px 8px;"">{email.OldDate:dddd, MMMM d, yyyy}</td>
              </tr>
              <tr style=""color: #999;"">
                <td style=""font-weight: bold; padding: 4px 8px;"">Time</td>
                <td style=""padding: 4px 8px;"">{email.OldTime:h:mm tt}</td>
              </tr>
              <tr style=""color: #999; border-bottom: 1px solid #eee;"">
                <td style=""font-weight: bold; padding: 4px 8px 12px 8px;"">Service</td>
                <td style=""padding: 4px 8px 12px 8px;"">{email.OldServiceName} ({email.OldServiceDuration} min)</td>
              </tr>
              <tr>
                <td colspan=""2"" style=""font-size: 11px; font-weight: bold; text-transform: uppercase; letter-spacing: 0.05em; color: #769A95; padding: 12px 8px 4px 8px;"">Updated to</td>
              </tr>
              <tr>
                <td style=""font-weight: bold; width: 90px; padding: 4px 8px;"">Date</td>
                <td style=""padding: 4px 8px;"">{email.Date:dddd, MMMM d, yyyy}</td>
              </tr>
              <tr>
                <td style=""font-weight: bold; padding: 4px 8px;"">Time</td>
                <td style=""padding: 4px 8px;"">{email.Time:h:mm tt}</td>
              </tr>
              <tr>
                <td style=""font-weight: bold; padding: 4px 8px;"">Service</td>
                <td style=""padding: 4px 8px;"">{email.ServiceName} ({email.ServiceDuration} min)</td>
              </tr>"
            : $@"<tr style=""border-bottom: 1px solid #eee;"">
                <td style=""font-weight: bold; width: 90px;"">Date</td>
                <td>{email.Date:dddd, MMMM d, yyyy}</td>
              </tr>
              <tr style=""border-bottom: 1px solid #eee;"">
                <td style=""font-weight: bold;"">Time</td>
                <td>{email.Time:h:mm tt}</td>
              </tr>
              <tr>
                <td style=""font-weight: bold;"">Service</td>
                <td>{email.ServiceName} ({email.ServiceDuration} min)</td>
              </tr>";

        return @$"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
  <tr>
    <td align=""center"">
      <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
        <tr>
          <td style=""background-color: #769A95; padding: 30px; text-align: center;"">
            <h1 style=""color: #ffffff; margin: 0; font-family: Arial, sans-serif; font-size: 28px;"">
              Shooper Dooper Nails
            </h1>
          </td>
        </tr>
        <tr>
          <td style=""padding: 40px 30px; font-family: Arial, sans-serif; color: #3F3F44;"">
            <h2 style=""color: #769A95; margin-top: 0;"">{heading}</h2>
            <p style=""font-size: 16px; line-height: 1.6; margin: 0 0 24px 0;"">{intro}</p>
            <table cellpadding=""8"" cellspacing=""0"" style=""border-collapse: collapse; width: 100%; font-size: 15px;"">
              {appointmentRows}
            </table>
          </td>
        </tr>
        <tr>
          <td style=""background-color: #f9f9f9; padding: 20px; text-align: center; font-family: Arial, sans-serif; font-size: 12px; color: #666666;"">
            <p style=""margin: 0;"">Follow us on Instagram: <a href=""https://www.instagram.com/shooperdoopernails/"" style=""color: #769A95;"">@shooperdoopernails</a></p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
    }

    private static string TextTechNotificationBody(TechNotificationEmail email, string headline)
    {
        if (email.OldDate is not null)
        {
            return @$"{headline}

Previous:
  Date:    {email.OldDate:dddd, MMMM d, yyyy}
  Time:    {email.OldTime:h:mm tt}
  Service: {email.OldServiceName} ({email.OldServiceDuration} min)

Updated to:
  Date:    {email.Date:dddd, MMMM d, yyyy}
  Time:    {email.Time:h:mm tt}
  Service: {email.ServiceName} ({email.ServiceDuration} min)

— Shooper Dooper Scheduling";
        }

        return @$"{headline}

Date:    {email.Date:dddd, MMMM d, yyyy}
Time:    {email.Time:h:mm tt}
Service: {email.ServiceName} ({email.ServiceDuration} min)

— Shooper Dooper Scheduling";
    }

    private static string HtmlTechNotificationBody(TechNotificationEmail email, string heading, string intro)
    {
        var appointmentRows = email.OldDate is not null
            ? $@"<tr>
                <td colspan=""2"" style=""font-size: 11px; font-weight: bold; text-transform: uppercase; letter-spacing: 0.05em; color: #999; padding: 8px 8px 4px 8px;"">Previous</td>
              </tr>
              <tr style=""color: #999;"">
                <td style=""font-weight: bold; width: 90px; padding: 4px 8px;"">Date</td>
                <td style=""padding: 4px 8px;"">{email.OldDate:dddd, MMMM d, yyyy}</td>
              </tr>
              <tr style=""color: #999;"">
                <td style=""font-weight: bold; padding: 4px 8px;"">Time</td>
                <td style=""padding: 4px 8px;"">{email.OldTime:h:mm tt}</td>
              </tr>
              <tr style=""color: #999; border-bottom: 1px solid #eee;"">
                <td style=""font-weight: bold; padding: 4px 8px 12px 8px;"">Service</td>
                <td style=""padding: 4px 8px 12px 8px;"">{email.OldServiceName} ({email.OldServiceDuration} min)</td>
              </tr>
              <tr>
                <td colspan=""2"" style=""font-size: 11px; font-weight: bold; text-transform: uppercase; letter-spacing: 0.05em; color: #769A95; padding: 12px 8px 4px 8px;"">Updated to</td>
              </tr>
              <tr>
                <td style=""font-weight: bold; width: 90px; padding: 4px 8px;"">Date</td>
                <td style=""padding: 4px 8px;"">{email.Date:dddd, MMMM d, yyyy}</td>
              </tr>
              <tr>
                <td style=""font-weight: bold; padding: 4px 8px;"">Time</td>
                <td style=""padding: 4px 8px;"">{email.Time:h:mm tt}</td>
              </tr>
              <tr>
                <td style=""font-weight: bold; padding: 4px 8px;"">Service</td>
                <td style=""padding: 4px 8px;"">{email.ServiceName} ({email.ServiceDuration} min)</td>
              </tr>"
            : $@"<tr style=""border-bottom: 1px solid #eee;"">
                <td style=""font-weight: bold; width: 90px;"">Date</td>
                <td>{email.Date:dddd, MMMM d, yyyy}</td>
              </tr>
              <tr style=""border-bottom: 1px solid #eee;"">
                <td style=""font-weight: bold;"">Time</td>
                <td>{email.Time:h:mm tt}</td>
              </tr>
              <tr>
                <td style=""font-weight: bold;"">Service</td>
                <td>{email.ServiceName} ({email.ServiceDuration} min)</td>
              </tr>";

        return @$"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
  <tr>
    <td align=""center"">
      <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
        <tr>
          <td style=""background-color: #769A95; padding: 30px; text-align: center;"">
            <h1 style=""color: #ffffff; margin: 0; font-family: Arial, sans-serif; font-size: 28px;"">
              Shooper Dooper Nails
            </h1>
          </td>
        </tr>
        <tr>
          <td style=""padding: 40px 30px; font-family: Arial, sans-serif; color: #3F3F44;"">
            <h2 style=""color: #769A95; margin-top: 0;"">{heading}</h2>
            <p style=""font-size: 16px; line-height: 1.6; margin: 0 0 24px 0;"">{intro}</p>
            <table cellpadding=""8"" cellspacing=""0"" style=""border-collapse: collapse; width: 100%; font-size: 15px;"">
              {appointmentRows}
            </table>
          </td>
        </tr>
        <tr>
          <td style=""background-color: #f9f9f9; padding: 20px; text-align: center; font-family: Arial, sans-serif; font-size: 12px; color: #666666;"">
            <p style=""margin: 0;"">Follow us on Instagram: <a href=""https://www.instagram.com/shooperdoopernails/"" style=""color: #769A95;"">@shooperdoopernails</a></p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetToken)
    {
        try
        {
            _logger.LogInformation("Sending password reset email to {EmailAddress}", toEmail);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Password Reset - Shooper Dooper Scheduling";

            var builder = new BodyBuilder();
            builder.TextBody = TextPasswordResetBody(resetToken);
            builder.HtmlBody = HtmlPasswordResetBody(resetToken);
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            client.ServerCertificateValidationCallback = (s, cert, chain, errors) => true;

            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Password reset email sent successfully to {EmailAddress}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {EmailAddress}", toEmail);
            throw new InvalidOperationException($"Failed to send password reset email to {toEmail}", ex);
        }
    }

    private static string TextPasswordResetBody(string resetToken)
    {
        return @$"You requested a password reset for your Shooper Dooper Scheduling account.

To set your password, please copy and paste the following link into your browser:

http://127.0.0.1:5173/auth/reset-password?token={resetToken}

This link will expire in 30 minutes. If you did not request this, you can safely ignore this email.";
    }

    private static string HtmlPasswordResetBody(string resetToken)
    {
        return @$"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
  <tr>
    <td align=""center"">
      <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
        <tr>
          <td style=""background-color: #769A95; padding: 30px; text-align: center;"">
            <h1 style=""color: #ffffff; margin: 0; font-family: Arial, sans-serif; font-size: 28px;"">
              Shooper Dooper Nails
            </h1>
          </td>
        </tr>
        <tr>
          <td style=""padding: 40px 30px; font-family: Arial, sans-serif; color: #3F3F44;"">
            <h2 style=""color: #769A95; margin-top: 0;"">Password Reset</h2>
            <p style=""font-size: 16px; line-height: 1.6; margin: 20px 0;"">
              You requested a password reset for your scheduling account. Click the button below to set your new password.
            </p>
            <table cellpadding=""0"" cellspacing=""0"" style=""margin: 30px 0;"">
              <tr>
                <td align=""center"" style=""background-color: #769A95; border-radius: 5px;"">
                  <a href=""http://127.0.0.1:5173/auth/reset-password?token={resetToken}""
                     style=""display: inline-block; padding: 15px 40px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; font-family: Arial, sans-serif;"">
                    Set Password
                  </a>
                </td>
              </tr>
            </table>
            <p style=""font-size: 14px; line-height: 1.6; color: #666666;"">
              This link will expire in 30 minutes. If you did not request this, you can safely ignore this email.
            </p>
          </td>
        </tr>
        <tr>
          <td style=""background-color: #f9f9f9; padding: 20px; text-align: center; font-family: Arial, sans-serif; font-size: 12px; color: #666666;"">
            <p style=""margin: 0;"">Follow us on Instagram: <a href=""https://www.instagram.com/shooperdoopernails/"" style=""color: #769A95;"">@shooperdoopernails</a></p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
    }

    private static string TextConfirmationLinkBody(string confirmationToken)
    {
        return @$"Thank you for booking an appointment at Shooper Dooper!
            
            To confirm your booking, please copy and paste the following link into your browser.
            
            http://127.0.0.1:5173/booking/confirm?token={confirmationToken}";
    }

    private static string HtmlConfirmationLinkBody(string confirmationToken)
    {
        return @$"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
  <tr>
    <td align=""center"">
      <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
        <!-- Header with brand color -->
        <tr>
          <td style=""background-color: #769A95; padding: 30px; text-align: center;"">
            <h1 style=""color: #ffffff; margin: 0; font-family: Arial, sans-serif; font-size: 28px;"">
              Shooper Dooper Nails
            </h1>
          </td>
        </tr>

        <!-- Content -->
        <tr>
          <td style=""padding: 40px 30px; font-family: Arial, sans-serif; color: #3F3F44;"">
            <h2 style=""color: #769A95; margin-top: 0;"">Thank you for booking!</h2>
            <p style=""font-size: 16px; line-height: 1.6; margin: 20px 0;"">
              We're excited to see you! Please confirm your appointment by clicking the button below.
            </p>

            <!-- CTA Button -->
            <table cellpadding=""0"" cellspacing=""0"" style=""margin: 30px 0;"">
              <tr>
                <td align=""center"" style=""background-color: #769A95; border-radius: 5px;"">
                  <a href=""http://127.0.0.1:5173/booking/confirm?token={confirmationToken}""
                     style=""display: inline-block; padding: 15px 40px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; font-family: Arial, sans-serif;"">
                    Confirm Appointment
                  </a>
                </td>
              </tr>
            </table>
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style=""background-color: #f9f9f9; padding: 20px; text-align: center; font-family: Arial, sans-serif; font-size: 12px; color: #666666;"">
            <p style=""margin: 0;"">Follow us on Instagram: <a href=""https://www.instagram.com/shooperdoopernails/"" style=""color: #769A95;"">@shooperdoopernails</a></p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
    }

    private static string TextFinalConfirmationBody(DateTime appointmentTime)
    {
        return @$"Thank you for choosing Shooper Dooper Nails!
        
        Your appointment has been confirmed for {appointmentTime.ToString("f")}";
    }

    private static string HtmlFinalConfirmationBody(DateTime appointmentTime)
    {
        return @$"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
            <tr>
                <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
                    <!-- Header with brand color -->
                    <tr>
                    <td style=""background-color: #769A95; padding: 30px; text-align: center;"">
                        <h1 style=""color: #ffffff; margin: 0; font-family: Arial, sans-serif; font-size: 28px;"">
                        Shooper Dooper Nails
                        </h1>
                    </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                    <td style=""padding: 40px 30px; font-family: Arial, sans-serif; color: #3F3F44;"">
                        <h2 style=""color: #769A95; margin-top: 0;"">Thank you for booking an appointment at Shooper Dooper!</h2>
                        <p style=""font-size: 16px; line-height: 1.6; margin: 20px 0;"">
                        Your appointment has been confirmed for {appointmentTime.ToString("f")}.
                        </p>
                    </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                    <td style=""background-color: #f9f9f9; padding: 20px; text-align: center; font-family: Arial, sans-serif; font-size: 12px; color: #666666;"">
                        <p style=""margin: 0;"">Follow us on Instagram: <a href=""https://www.instagram.com/shooperdoopernails/"" style=""color: #769A95;"">@shooperdoopernails</a></p>
                    </td>
                    </tr>
                </table>
                </td>
            </tr>
        </table>";
    }
}