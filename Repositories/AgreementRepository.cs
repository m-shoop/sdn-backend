using SdnBackend.Models;
using Npgsql;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SdnBackend.Repositories;

public class AgreementRepository(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<int> SaveEntity(Agreement agreement)
    {
        await using var command = _dataSource.CreateCommand(@"INSERT INTO agreements 
        (date, start_time, service_id, tech_id, client_id, salon_id, status, expires_at, confirmation_token_hash, creation_timestamp)
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10);");

        command.Parameters.AddWithValue(agreement.Date); // $1
        command.Parameters.AddWithValue(agreement.Time); // $2
        command.Parameters.AddWithValue(agreement.Service.Id); // $3
        command.Parameters.AddWithValue(agreement.Tech.Id); // $4
        command.Parameters.AddWithValue(agreement.Client.Id); // $5
        command.Parameters.AddWithValue(agreement.Salon.Id); // $6
        command.Parameters.AddWithValue((int)agreement.ApptStatus); // $7

        if (agreement.ApptStatus == AppointmentStatus.pending)
        {
            if (agreement.ExpireTimestamp == null || agreement.ConfirmTokenHash == null)
            {
                throw new InvalidOperationException("Pending appointments must have a token hash and expiry timestamp.");
            }
            command.Parameters.AddWithValue(agreement.ExpireTimestamp); // $8
            command.Parameters.AddWithValue(agreement.ConfirmTokenHash); // $9
            command.Parameters.AddWithValue(agreement.CreateTimestamp); // $10
        }

        // TODO: implement confirmed, canceled and expired statuses
        else
        {
            throw new InvalidOperationException("Other appointment statuses not yet implemented.");
        }      

        try
        {
            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        
    }

    public async Task<int> UpdateEntity(Agreement agreement)
    {
        await using var command = _dataSource.CreateCommand(@"UPDATE agreements 
        SET date = $1, start_time = $2, service_id = $3, tech_id = $4, client_id = $5, 
        salon_id = $6, status = $7, expires_at = $8, confirmed_at = $9, confirmation_token_hash = $10, creation_timestamp = $11
        WHERE id = $12;");

        command.Parameters.AddWithValue(agreement.Date); // $1
        command.Parameters.AddWithValue(agreement.Time); // $2
        command.Parameters.AddWithValue(agreement.Service?.Id ?? (object)DBNull.Value); // $3
        command.Parameters.AddWithValue(agreement.Tech?.Id ?? (object)DBNull.Value); // $4
        command.Parameters.AddWithValue(agreement.Client?.Id ?? (object)DBNull.Value); // $5
        command.Parameters.AddWithValue(agreement.Salon?.Id ?? (object)DBNull.Value); // $6
        command.Parameters.AddWithValue((int)agreement.ApptStatus); // $7
        command.Parameters.AddWithValue(agreement?.ExpireTimestamp ?? (object)DBNull.Value); // $8
        command.Parameters.AddWithValue(agreement?.ConfirmTimestamp ?? (object)DBNull.Value); // $9
        command.Parameters.AddWithValue(agreement?.ConfirmTokenHash ?? (object)DBNull.Value); // $10
        command.Parameters.AddWithValue(agreement?.CreateTimestamp ?? (object)DBNull.Value); // $11
        command.Parameters.AddWithValue(agreement?.Id ?? (object)DBNull.Value); // $12

        try
        {
            var affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    /// <summary>
    /// Returns a List<Agreement> object with all Agreement objects for the provided
    /// Technician and on the provided date
    /// </summary>
    /// <param name="date">date for agreements</param>
    /// <param name="tech">only Agreements for this Technician are included</param>
    /// <returns>Task for a List<Agreement> object</returns>
    public async Task<List<Agreement>> GetActiveAgreementsForTechOnDate(DateOnly date, Technician tech)
    {
        List<Agreement> listAgreements = new List<Agreement>();
        const int PENDING_STATUS = 0;
        const int CONFIRMED_STATUS = 1;

        // 
        await using var command = _dataSource.CreateCommand(@"SELECT * FROM agreements WHERE
            date=$1 AND tech_id=$2 AND (status=$3 OR status=$4);");

        command.Parameters.AddWithValue(date); // $1
        command.Parameters.AddWithValue(tech.Id); // $2
        command.Parameters.AddWithValue(PENDING_STATUS); // $3
        command.Parameters.AddWithValue(CONFIRMED_STATUS); // $4

        await using var reader = await command.ExecuteReaderAsync(); 

        while (await reader.ReadAsync())
        {
            Agreement? agreement = await ReadEntityFromReader(reader);
            if (agreement != null)
            {
                listAgreements.Add(agreement);
            }
        }

        return listAgreements;

    }

    /// <summary>
    /// Reads a single Agreement from the SQL database from a NpgsqlDataReader object
    /// </summary>
    /// <param name="reader">NpgsqlDataReader object</param>
    /// <returns>Task for Agreement?</returns>
    public async Task<Agreement?> ReadEntityFromReader(NpgsqlDataReader reader)
    {
        // build the necessary repositories first
        SalonRepository salonRepo = new SalonRepository(_dataSource);
        AccountRepository accountRepo = new AccountRepository(_dataSource);
        ServiceRepository serviceRepo = new ServiceRepository(_dataSource);

        // now read out the raw values from SQL
        // TODO: reactor for null handling
        var id = reader.GetInt32(reader.GetOrdinal("id"));
        DateOnly date = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("date"));
        TimeOnly startTime = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("start_time"));
        int serviceId = reader.GetInt32(reader.GetOrdinal("service_id"));
        int techId = reader.GetInt32(reader.GetOrdinal("tech_id"));
        int clientId = reader.GetInt32(reader.GetOrdinal("client_id"));
        int salonId = reader.GetInt32(reader.GetOrdinal("salon_id"));
        int expiresAtOrdinal = reader.GetOrdinal("expires_at");
        DateTime? expiresTs = reader.IsDBNull(expiresAtOrdinal)
            ? null
            : reader.GetFieldValue<DateTime>(expiresAtOrdinal);
        int confirmedAtOrdinal = reader.GetOrdinal("confirmed_at");
        DateTime? confirmedTs = reader.IsDBNull(confirmedAtOrdinal)
            ? null
            : reader.GetFieldValue<DateTime>(confirmedAtOrdinal);
        int confirmationTokenHashOrdinal = reader.GetOrdinal("confirmation_token_hash");
        string? confTokenHash = reader.IsDBNull(confirmationTokenHashOrdinal)
            ? null
            : reader.GetString(confirmationTokenHashOrdinal);
        DateTime creationTs = reader.GetFieldValue<DateTime>(reader.GetOrdinal("creation_timestamp"));
        AppointmentStatus apptStatus = (AppointmentStatus)reader.GetInt32(reader.GetOrdinal("status"));

        // now build the full objects needed for an Agreement (Salon, Service, Tech, Client)
        Salon? salon = await salonRepo.GetById(salonId);
        Service? service = await serviceRepo.GetById(serviceId);
        Account? techAccount = await accountRepo.GetById(techId);
        Account? clientAccount = await accountRepo.GetById(clientId);

        // make sure we have required properties
        // TODO: add errors/exceptions to pass up?
        if (techAccount is not Technician tech || clientAccount is not Client client)
        {
            return null;
        }

        // what about the date and time?
        // TODO: handle null inputs from Npgsql's return for the reader
        if (salon == null || service == null)
        {
            return null;
        }

        Agreement agreement = new Agreement
        {
            Id = id,
            Date = date,
            Time = startTime,
            Service = service,
            Tech = tech,
            Client = client,
            Salon = salon,
            ApptStatus = apptStatus,
            ExpireTimestamp = expiresTs,
            ConfirmTimestamp = confirmedTs,
            ConfirmTokenHash = confTokenHash,
            CreateTimestamp = creationTs
        };

        return agreement;
        
    }

    public async Task<Agreement?> GetById(int id)
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM agreements WHERE id = $1;");
        command.Parameters.AddWithValue(id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
            return await ReadEntityFromReader(reader);

        return null;
    }

    public async Task<List<Agreement>> GetActiveAgreementsForTechOnDateExcluding(DateOnly date, Technician tech, int excludeId)
    {
        var listAgreements = new List<Agreement>();
        const int PENDING_STATUS = 0;
        const int CONFIRMED_STATUS = 1;

        await using var command = _dataSource.CreateCommand(@"SELECT * FROM agreements WHERE
            date=$1 AND tech_id=$2 AND (status=$3 OR status=$4) AND id != $5;");

        command.Parameters.AddWithValue(date);
        command.Parameters.AddWithValue(tech.Id);
        command.Parameters.AddWithValue(PENDING_STATUS);
        command.Parameters.AddWithValue(CONFIRMED_STATUS);
        command.Parameters.AddWithValue(excludeId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Agreement? agreement = await ReadEntityFromReader(reader);
            if (agreement != null)
                listAgreements.Add(agreement);
        }

        return listAgreements;
    }

    public async Task CancelAgreement(int id)
    {
        await using var command = _dataSource.CreateCommand(
            "UPDATE agreements SET status = $1 WHERE id = $2;");
        command.Parameters.AddWithValue((int)AppointmentStatus.cancelled);
        command.Parameters.AddWithValue(id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Agreement?> GetByConfirmationTokenHash(string confirmationTokenHash)
    {
        confirmationTokenHash = confirmationTokenHash.Trim();
        await using var command = _dataSource.CreateCommand(@"SELECT * FROM agreements WHERE
        confirmation_token_hash = $1;");

        command.Parameters.AddWithValue(confirmationTokenHash); // $1

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            Agreement? agreement = await ReadEntityFromReader(reader);
            return agreement;
        }
        return null;
    }

    /// <summary>
    /// Returns all pending appointments that have passed their expiration timestamp
    /// </summary>
    /// <returns>List of expired pending agreements</returns>
    public async Task<List<Agreement>> GetExpiredPendingAgreements()
    {
        List<Agreement> expiredAgreements = new List<Agreement>();
        const int PENDING_STATUS = 0; // AppointmentStatus.pending

        await using var command = _dataSource.CreateCommand(@"SELECT * FROM agreements
            WHERE status = $1 AND expires_at IS NOT NULL AND expires_at < $2;");

        command.Parameters.AddWithValue(PENDING_STATUS); // $1
        command.Parameters.AddWithValue(DateTime.Now); // $2

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Agreement? agreement = await ReadEntityFromReader(reader);
            if (agreement != null)
            {
                expiredAgreements.Add(agreement);
            }
        }

        return expiredAgreements;
    }

    public async Task<int> DeleteAgreementsOlderThan(DateOnly cutoff)
    {
        await using var command = _dataSource.CreateCommand(@"
            DELETE FROM agreements WHERE date < $1;");
        command.Parameters.AddWithValue(cutoff);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> CreateConfirmedAgreement(Agreement agreement)
    {
        await using var command = _dataSource.CreateCommand(@"
            INSERT INTO agreements (date, start_time, service_id, tech_id, client_id, salon_id, status, creation_timestamp)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
            RETURNING id;");
        command.Parameters.AddWithValue(agreement.Date);
        command.Parameters.AddWithValue(agreement.Time);
        command.Parameters.AddWithValue(agreement.Service.Id);
        command.Parameters.AddWithValue(agreement.Tech.Id);
        command.Parameters.AddWithValue(agreement.Client.Id);
        command.Parameters.AddWithValue(agreement.Salon.Id);
        command.Parameters.AddWithValue((int)AppointmentStatus.confirmed);
        command.Parameters.AddWithValue(agreement.CreateTimestamp);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}