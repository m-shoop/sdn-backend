using SdnBackend.Models;
using Npgsql;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SdnBackend.Repositories;

public class AccountRepository(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<List<Account>> GetAll()
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM accounts;");

        await using var reader = await command.ExecuteReaderAsync();

        var accountList = new List<Account>();

        while (await reader.ReadAsync())
        {
            Task<Account?> taskAccount = ReadEntityFromReader(reader);
            Account? account = await taskAccount;
            if (account is not null)
            {
                accountList.Add(account);
            }
        }

        return accountList;
    }

    public async Task<Account?> GetById(int id)
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM accounts WHERE account_id = $1;");
        command.Parameters.AddWithValue(id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            // call read method
            var taskAccount = ReadEntityFromReader(reader);
            return await taskAccount;
        }

        return null;
    }

    private async Task<Account?> ReadEntityFromReader(NpgsqlDataReader reader)
    {
            var id = reader.GetInt32(reader.GetOrdinal("account_id"));
            var name = reader.GetString(reader.GetOrdinal("name"));
            var email = reader.GetString(reader.GetOrdinal("email"));
            var role = reader.GetString(reader.GetOrdinal("role"));

            if (role == "Technician"){
                Technician tech = new(id, name, email);
                var taskServiceList = GetServiceListForTechnician(tech);
                tech.Services = await taskServiceList;
                return tech;
            }
            // implement administrator later

            if (role == "Client"){
                int lastActivityOrdinal = reader.GetOrdinal("last_activity");
                DateTime? lastActivity = reader.IsDBNull(lastActivityOrdinal)
                    ? null
                    : reader.GetFieldValue<DateTime>(lastActivityOrdinal);
                Client client = new(id, name, email) { LastActivity = lastActivity };
                return client;
            }

            // add logic if role is empty or not valid
            return null;
    }

    private async Task<List<Service>> GetServiceListForTechnician(Technician tech)
    {
        // first pull the service_ids, then pull the meta-information to create them as Service resources, then 
        // return the list of them
        // so two commands to PostgreSQL
        List<Service> services = new List<Service>();

        await using var command = _dataSource.CreateCommand(@"
            SELECT s.service_id, s.service_name, s.service_duration, s.maximum_participants
            FROM services s 
            JOIN tech_services ts ON ts.service_id = s.service_id
            WHERE ts.tech_id = $1
        ");

        command.Parameters.AddWithValue(tech.Id);

        await using var reader = await command.ExecuteReaderAsync();

        while ( await reader.ReadAsync() )
        {
            var serviceId = reader.GetInt32(reader.GetOrdinal("service_id"));
            var serviceName = reader.GetString(reader.GetOrdinal("service_name"));
            var serviceDuration = reader.GetInt32(reader.GetOrdinal("service_duration"));

            Service service = new Service(serviceId, serviceName, serviceDuration);
            services.Add(service);
        }

        return services;
    }

    public async Task<int> SaveEntity(Client client)
    {
        await using var command = _dataSource.CreateCommand(@"INSERT INTO accounts 
        (name, email, role) VALUES ($1, $2, $3);");
        command.Parameters.AddWithValue(client.Name);
        command.Parameters.AddWithValue(client.Email);
        command.Parameters.AddWithValue(client.Role);

        var affectedRows = await command.ExecuteNonQueryAsync();
        return affectedRows;
    }

    // TODO: this needs to return a potential list of accounts, not just one account
    public async Task<Account?> GetByEmailAndRole(string email, string role)
    {
        await using var command = _dataSource.CreateCommand(@"SELECT * FROM
            accounts WHERE email = $1 AND role = $2;");
        command.Parameters.AddWithValue(email); // $1
        command.Parameters.AddWithValue(role); // $2

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return await ReadEntityFromReader(reader);
        }

        else return null;
    }

    // --- Auth methods ---

    public async Task<string?> GetPasswordHash(int accountId)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT password_hash FROM accounts WHERE account_id = $1;");
        command.Parameters.AddWithValue(accountId);

        var result = await command.ExecuteScalarAsync();
        return result as string;
    }

    public async Task SetPasswordHash(int accountId, string passwordHash)
    {
        await using var command = _dataSource.CreateCommand(@"
            UPDATE accounts
            SET password_hash = $1,
                password_reset_token_hash = NULL,
                password_reset_token_expires = NULL
            WHERE account_id = $2;");
        command.Parameters.AddWithValue(passwordHash);
        command.Parameters.AddWithValue(accountId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task SetResetToken(int accountId, string tokenHash, DateTime expires)
    {
        await using var command = _dataSource.CreateCommand(@"
            UPDATE accounts
            SET password_reset_token_hash = $1,
                password_reset_token_expires = $2
            WHERE account_id = $3;");
        command.Parameters.AddWithValue(tokenHash);
        command.Parameters.AddWithValue(expires);
        command.Parameters.AddWithValue(accountId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<Account?> GetByResetTokenHash(string tokenHash)
    {
        await using var command = _dataSource.CreateCommand(@"
            SELECT * FROM accounts
            WHERE password_reset_token_hash = $1
              AND password_reset_token_expires > $2;");
        command.Parameters.AddWithValue(tokenHash);
        command.Parameters.AddWithValue(DateTime.UtcNow);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return await ReadEntityFromReader(reader);
        }

        return null;
    }

    public async Task<bool> GetEmailNotificationPreference(int techId)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT email_notifications_enabled FROM accounts WHERE account_id = $1;");
        command.Parameters.AddWithValue(techId);

        var result = await command.ExecuteScalarAsync();
        return result is bool b && b;
    }

    public async Task SetEmailNotificationPreference(int techId, bool enabled)
    {
        await using var command = _dataSource.CreateCommand(@"
            UPDATE accounts SET email_notifications_enabled = $1 WHERE account_id = $2;");
        command.Parameters.AddWithValue(enabled);
        command.Parameters.AddWithValue(techId);

        await command.ExecuteNonQueryAsync();
    }

    // --- GDPR methods ---

    public async Task TouchLastActivity(int clientId)
    {
        await using var command = _dataSource.CreateCommand(@"
            UPDATE accounts SET last_activity = $1 WHERE account_id = $2;");
        command.Parameters.AddWithValue(DateTime.UtcNow);
        command.Parameters.AddWithValue(clientId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteInactiveClientAccounts(DateTime cutoff)
    {
        await using var command = _dataSource.CreateCommand(@"
            DELETE FROM accounts
            WHERE role = 'Client'
              AND last_activity IS NOT NULL
              AND last_activity < $1;");
        command.Parameters.AddWithValue(cutoff);

        return await command.ExecuteNonQueryAsync();
    }
}