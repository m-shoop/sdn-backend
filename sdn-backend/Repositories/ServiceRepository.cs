using SdnBackend.Models;
using Npgsql;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SdnBackend.Repositories;

public class ServiceRepository(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<Service?> GetById(int serviceId)
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM services WHERE service_id = $1");
        command.Parameters.AddWithValue(serviceId);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            Service? service = ReadEntityFromReader(reader);
            return service;
        }

        return null;
    }

    public async Task<List<Service>> GetAll()
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM services;");
        await using var reader = await command.ExecuteReaderAsync();
        var services = new List<Service>();

        while (await reader.ReadAsync())
        {
            Service? service = ReadEntityFromReader(reader);
            if (service is not null)
                services.Add(service);
        }

        return services;
    }

    public Service? ReadEntityFromReader(NpgsqlDataReader reader)
    {
        var serviceId = reader.GetInt32(reader.GetOrdinal("service_id"));
        var serviceName = reader.GetString(reader.GetOrdinal("service_name"));
        var serviceDuration = reader.GetInt32(reader.GetOrdinal("service_duration"));
        var maxParticipants = reader.GetInt32(reader.GetOrdinal("maximum_participants"));

        Service? service = new Service(serviceId, serviceName, serviceDuration);
        if (service is not null && maxParticipants is not 0)
        {
            service.MaxParticipants = maxParticipants;
        }

        return service;
    }
}