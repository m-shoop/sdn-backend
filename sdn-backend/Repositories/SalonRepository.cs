using SdnBackend.Models;
using Npgsql;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SdnBackend.Repositories;

public class SalonRepository(NpgsqlDataSource dataSource)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<Salon?> GetById(int id)
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM salons WHERE salon_id = $1");
        command.Parameters.AddWithValue(id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Salon(id);
        }

        return null;
    }
    public async Task<IEnumerable<Salon>> GetAll()
    {
        var salons = new List<Salon>();

        await using var command = _dataSource.CreateCommand("SELECT * FROM salons");
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(reader.GetOrdinal("salon_id"));
            salons.Add(new Salon(id));
        }

        return salons;
    }

        /*void Add(Salon salon);
        void AddRange(IEnumerable<Salon> salons);

        void Remove(Salon salon);
        void RemoveRange(IEnumerable<Salon> salons);*/
}