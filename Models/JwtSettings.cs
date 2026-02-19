namespace SdnBackend.Models;

public class JwtSettings
{
    public required string SecretKey { get; set; }
    public int ExpirationMinutes { get; set; } = 30;
}
