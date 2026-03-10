using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SdnBackend.Models;
using SdnBackend.Repositories;

namespace SdnBackend.Services;

public class AuthService(
    NpgsqlDataSource dataSource,
    IEmailService emailService,
    JwtSettings jwtSettings,
    ILogger<AuthService> logger)
{
    private readonly AccountRepository _accountRepo = new(dataSource);

    public async Task RequestPasswordReset(string email)
    {
        var account = await _accountRepo.GetByEmailAndRole(email, "Technician");
        if (account is null)
        {
            // Don't reveal whether the email exists
            logger.LogInformation("Password reset requested for unknown email {Email}", email);
            return;
        }

        // Generate a random token (same pattern as Agreement.MarkPending)
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Base64UrlEncode(tokenBytes);
        var tokenHash = HashToken(token);
        var expires = DateTime.UtcNow.AddMinutes(30);

        await _accountRepo.SetResetToken(account.Id, tokenHash, expires);
        await emailService.SendPasswordResetAsync(email, token);

        logger.LogInformation("Password reset token sent for account {AccountId}", account.Id);
    }

    public async Task<(bool Success, string? ErrorMessage)> SetPassword(string token, string newPassword)
    {
        var tokenHash = HashToken(token);
        var account = await _accountRepo.GetByResetTokenHash(tokenHash);

        if (account is null)
        {
            logger.LogWarning("Set password attempted with invalid or expired token");
            return (false, "Invalid or expired reset token");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _accountRepo.SetPasswordHash(account.Id, passwordHash);

        logger.LogInformation("Password set successfully for account {AccountId}", account.Id);
        return (true, null);
    }

    public async Task<string?> Login(string email, string password)
    {
        var account = await _accountRepo.GetByEmailAndRole(email, "Technician");
        if (account is null)
            return null;

        var storedHash = await _accountRepo.GetPasswordHash(account.Id);
        if (storedHash is null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, storedHash))
            return null;

        return GenerateJwt(account);
    }

    private string GenerateJwt(Account account)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, account.Name),
            new Claim("role", account.Role)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
