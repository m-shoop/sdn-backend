namespace SdnBackend.Dtos;

public record TechSettingsDto(bool EmailNotificationsEnabled);
public record UpdateTechSettingsRequest(bool EmailNotificationsEnabled);
