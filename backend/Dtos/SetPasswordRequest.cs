namespace SdnBackend.Dtos;

public record SetPasswordRequest(string Token, string NewPassword);
