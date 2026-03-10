namespace SdnBackend.Dtos;

public record AvailabilityRequest(string Salon, string Service, string DateBegin, string DateEnd);
