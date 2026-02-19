namespace SdnBackend.Dtos;

public record BookingRequest(
    string AgreementDate,
    string StartTime,
    string Service,
    string Tech,
    string Salon,
    string ClientEmail,
    string ClientName);
