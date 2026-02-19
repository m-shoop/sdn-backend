using SdnBackend.Models;
using SdnBackend.Repositories;
using Npgsql;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SdnBackend.Services;

public class AgreementService(AgreementRepository agreeRepo, ServiceRepository servRepo, AccountRepository accountRepo, SalonRepository salonRepo)
{
    private readonly AgreementRepository _agreeRepo = agreeRepo;
    private readonly ServiceRepository _servRepo = servRepo;
    private readonly AccountRepository _accountRepo = accountRepo;
    private readonly SalonRepository _salonRepo = salonRepo;

    /// <summary>
    /// CreateAgreement method to help with the building of Agreement objects
    /// prior to handing them over to the Repository to commit to the database
    /// </summary>
    /// <param name="date">string representation of a date from the frontend</param>
    /// <param name="time">string representation of a time from the frontend</param>
    /// <param name="serviceId">integer id of a service; should already exist in the database</param>
    /// <param name="techId">integer id of a technician, should already exist in the database</param>
    /// <param name="clientId">integer id of a client, should already exist in the database</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">If the serviceId, techId, or clientId do not exist in database
    /// then this exception will be thrown. See DateOnly and TimeOnly.ParseExact's documentation for other exceptions 
    /// that can be thrown</exception>
    public async Task<Agreement?> CreateAgreement(string date, string time, int serviceId, int techId, int clientId, int salonId)
    {
        DateOnly agreementDate = DateOnly.ParseExact(date, "yyyyMMdd");
        TimeOnly agreementTime = TimeOnly.ParseExact(time, "HH:mm"); // totally not sure about this, Microsoft's documentation isn't very good..

        Service? service = await _servRepo.GetById(serviceId) ?? throw new InvalidOperationException(
                $"Service {serviceId} does not exist in database."
            );
        Account? techAccount = await _accountRepo.GetById(techId) ?? throw new InvalidOperationException(
                $"Tech {techId} does not exist in database."
            );
        Account? clientAccount = await _accountRepo.GetById(clientId) ?? throw new InvalidOperationException(
                $"Client {clientId} does not exist in database."
            );

        if (techAccount is not Technician tech)
        {
            throw new InvalidOperationException(
                $"Account {techAccount} is not a Technician"
            );
        }

        if (clientAccount is not Client client)
        {
            throw new InvalidOperationException(
                $"Account {clientAccount} is not a Client"
            );
        }

        Salon? salon = await _salonRepo.GetById(salonId) ?? throw new InvalidOperationException(
                $"Salon {salonId} does not exist in database."
            );

        // return new Agreement object
        return new Agreement
        {
            Date = agreementDate,
            Time = agreementTime,
            Service = service,
            Tech = tech,
            Client = client,
            Salon = salon
        };
    }
}