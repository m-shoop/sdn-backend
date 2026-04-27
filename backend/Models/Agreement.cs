using System.Security.Cryptography;
using System.Text;

namespace SdnBackend.Models;

public enum AppointmentStatus
{
    pending,
    confirmed,
    expired,
    cancelled
}

public class Agreement
{
    const int EXPIRATION_HOLD_IN_MINUTES = 30;
    public int Id { get; set; }

    public required DateOnly Date { get; init; }
    public required TimeOnly Time { get; init; }
    public required Service Service { get; init; }
    public required Technician Tech { get; init; }
    public required Client Client { get; init; }
    public required Salon Salon { get; init; }

    public AppointmentStatus ApptStatus { get; set; }

    public DateTime? ExpireTimestamp { get; set; }
    public DateTime? ConfirmTimestamp { get; set; }
    public string? ConfirmTokenHash { get; set; }

    public DateTime CreateTimestamp { get; init; } = DateTime.UtcNow;

    public string MarkPending()
    {
        ApptStatus = AppointmentStatus.pending;
        // always generate a new confirmation token if we're marking appointment as pending
        string confirmationToken = GenerateConfirmationToken();
        ConfirmTokenHash = HashToken(confirmationToken);
        ExpireTimestamp = DateTime.Now.AddMinutes(EXPIRATION_HOLD_IN_MINUTES);
        return confirmationToken;
    }

    public void Confirm()
    {
        if (ApptStatus != AppointmentStatus.pending)
            throw new InvalidOperationException("Only pending agreements can be confirmed.");
        
        ApptStatus = AppointmentStatus.confirmed;
        ConfirmTimestamp = DateTime.Now;
        ExpireTimestamp = null;
        // ConfirmTokenHash = null; // ensure this Token Hash can no longer be used
        // let's try keeping the hash so that confirmation link is idempotent
    }

    public void Expire()
    {
        ApptStatus = AppointmentStatus.expired;
    }

    private static string GenerateConfirmationToken(int byteLength = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
    public static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash); // or Base64
    }
}