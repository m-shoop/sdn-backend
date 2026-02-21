using System.Security.Cryptography;
using System.Text;
using SdnBackend.Models;
using SdnBackend.Repositories;

namespace SdnBackend.Services;

public class IcsFeedService(JwtSettings jwtSettings, AgreementRepository agreementRepository)
{
    private readonly JwtSettings _jwtSettings = jwtSettings;
    private readonly AgreementRepository _agreementRepository = agreementRepository;

    public string ComputeFeedToken(int techId)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        var dataBytes = Encoding.UTF8.GetBytes(techId.ToString());
        var hmac = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(hmac)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public bool ValidateToken(int techId, string token)
    {
        var expected = ComputeFeedToken(techId);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(token));
    }

    public string BuildFeedUrl(int techId, HttpRequest request)
    {
        var token = ComputeFeedToken(techId);
        var host = $"{request.Scheme}://{request.Host}";
        return $"{host}/calendar/feed/{techId}/appointments.ics?token={token}";
    }

    public async Task<string> GenerateIcs(int techId)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var agreements = await _agreementRepository.GetConfirmedAgreementsForTech(techId, cutoff);

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Shooper Dooper Nails//Appointments//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("X-WR-CALNAME:SDN Appointments");

        foreach (var appt in agreements)
        {
            var tz = appt.Salon.TimeZone;

            // Combine date + time as local Amsterdam time, then convert to UTC
            var localStart = new DateTime(
                appt.Date.Year, appt.Date.Month, appt.Date.Day,
                appt.Time.Hour, appt.Time.Minute, appt.Time.Second,
                DateTimeKind.Unspecified);
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
            var utcEnd = utcStart.AddMinutes(appt.Service.Duration);

            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:appt-{appt.Id}@shooperdoopernails.com");
            sb.AppendLine(Fold($"DTSTART:{utcStart:yyyyMMddTHHmmssZ}"));
            sb.AppendLine(Fold($"DTEND:{utcEnd:yyyyMMddTHHmmssZ}"));
            sb.AppendLine(Fold($"SUMMARY:{appt.Service.Name} \u2013 {appt.Client.Name}"));
            sb.AppendLine(Fold($"DESCRIPTION:{appt.Service.Duration} min"));
            sb.AppendLine("STATUS:CONFIRMED");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    // RFC 5545 §3.1: fold lines longer than 75 octets
    private static string Fold(string line)
    {
        if (line.Length <= 75)
            return line;

        var result = new StringBuilder();
        result.Append(line[..75]);
        var pos = 75;
        while (pos < line.Length)
        {
            result.Append("\r\n ");
            var take = Math.Min(74, line.Length - pos);
            result.Append(line.AsSpan(pos, take));
            pos += take;
        }
        return result.ToString();
    }
}
