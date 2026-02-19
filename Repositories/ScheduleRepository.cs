using SdnBackend.Models;
using Npgsql;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SdnBackend.Repositories;

public class ScheduleRepository(NpgsqlDataSource dataSource, AccountRepository accountRepo)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;

    public async Task<List<Schedule>> GetAll()
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM schedules;");

        await using var reader = await command.ExecuteReaderAsync();

        List<Schedule> scheduleList = new List<Schedule>();

        while (await reader.ReadAsync())
        {
            Task<Schedule?> scheduleTask = ReadEntityFromReader(reader);
            Schedule? schedule = await scheduleTask;
            if (schedule is not null)
            {
                scheduleList.Add(schedule);
            }
        }

        return scheduleList;
    }

    // GetById method returns Schedule? Task resource based on schedule_id lookup in schedules table
    // schedule_id is a primary key in SQL table schedules
    public async Task<Schedule?> GetById(int id)
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM schedules WHERE schedule_id = $1");
        command.Parameters.AddWithValue(id);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            Task<Schedule?> scheduleTask = ReadEntityFromReader(reader);
            Schedule? schedule = await scheduleTask;
            return schedule;
        }

        return null;
    }

    // GetDayTimeRangesForScheduleId method returns List<DayTimeRange> Task object based on schedule_id lookup in day_time_range table
    public async Task<List<DayTimeRange>> GetDayTimeRangesForScheduleId(int id)
    {
        await using var rangeCommand = _dataSource.CreateCommand("SELECT * FROM day_time_range WHERE schedule_id = $1");
        rangeCommand.Parameters.AddWithValue(id);

        await using var rangeReader = await rangeCommand.ExecuteReaderAsync(); 
        var dayTimeRanges = new List<DayTimeRange>();

        while (await rangeReader.ReadAsync())
        {
            var dayTimeRangeId = rangeReader.GetInt32(rangeReader.GetOrdinal("day_time_range_id"));
            var dayOfWeek = rangeReader.GetInt32(rangeReader.GetOrdinal("day_of_week"));
            TimeOnly startTime = rangeReader.GetFieldValue<TimeOnly>(rangeReader.GetOrdinal("start_time"));
            TimeOnly endTime = rangeReader.GetFieldValue<TimeOnly>(rangeReader.GetOrdinal("end_time"));
            DayOfWeek day = (DayOfWeek) dayOfWeek;
            DayTimeRange dayTimeRange = new(dayTimeRangeId, startTime, endTime, day);
            dayTimeRanges.Add(dayTimeRange);
        }

        return dayTimeRanges;
    }

    // GetSchedulesBySalon method returns List<Schedule> Task object based on salon_id in schedules table
    public async Task<List<Schedule>> GetSchedulesBySalon(Salon salon)
    {
        var salonId = salon.Id;
        await using var command = _dataSource.CreateCommand("SELECT * FROM schedules WHERE salon_id = $1");
        command.Parameters.AddWithValue(salonId);

        await using var reader = await command.ExecuteReaderAsync();
        var scheduleList = new List<Schedule>();

        var ordinal = reader.GetOrdinal("schedule_id");
        while (await reader.ReadAsync())
        {
            Schedule? schedule = await GetById(reader.GetInt32(ordinal));
            if (schedule is not null)
            {
                scheduleList.Add(schedule);
            }
        }

        return scheduleList;
    }

    public async Task<List<Schedule>> GetByTechId(int techId)
    {
        await using var command = _dataSource.CreateCommand("SELECT * FROM schedules WHERE account_id = $1");
        command.Parameters.AddWithValue(techId);

        await using var reader = await command.ExecuteReaderAsync();
        var scheduleList = new List<Schedule>();

        var ordinal = reader.GetOrdinal("schedule_id");
        while (await reader.ReadAsync())
        {
            Schedule? schedule = await GetById(reader.GetInt32(ordinal));
            if (schedule is not null)
            {
                scheduleList.Add(schedule);
            }
        }

        return scheduleList;
    }

    private async Task<Schedule?> ReadEntityFromReader(NpgsqlDataReader reader)
    {
            var scheduleId = reader.GetInt32(reader.GetOrdinal("schedule_id")); 
            // first retrieve the DayTimeRange objects from the day_time_range SQL table
            // if that succeeds, THEN proceed with building the Schedule object
            List<DayTimeRange> dayTimeRanges = await this.GetDayTimeRangesForScheduleId(scheduleId);
            var techId = reader.GetInt32(reader.GetOrdinal("account_id"));
            // pull the full Account object using the repository
            Task<Account?> taskAccount = accountRepo.GetById(techId);
            Account? account = await taskAccount;
            if (account is not Technician tech)
            {
                throw new InvalidOperationException(
                    $"Account {techId} is not a Technician"
                );
            }
            // retrieve Salon using SalonRepository
            var salonId = reader.GetInt32(reader.GetOrdinal("salon_id"));
            var salonRepo = new SalonRepository(_dataSource);
            Task<Salon?> salonTask = salonRepo.GetById(salonId);
            Salon? salon = await salonTask;
            // other simple column retrieves
            var numDaysPriorReleased = reader.GetInt32(reader.GetOrdinal("release_schedule"));
            DateOnly effStartDate = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("effective_start_date"));
            var frequency = reader.GetInt32(reader.GetOrdinal("frequency"));

            // read nullable effective_end_date
            var effEndDateOrdinal = reader.GetOrdinal("effective_end_date");
            DateOnly? effEndDate = reader.IsDBNull(effEndDateOrdinal)
                ? null
                : reader.GetFieldValue<DateOnly>(effEndDateOrdinal);

            // read outage flag
            bool outage = reader.GetBoolean(reader.GetOrdinal("outage"));

            if (salon is not null)
            {
                Schedule schedule = new(tech, salon, numDaysPriorReleased, effStartDate, dayTimeRanges, frequency)
                {
                    Id = scheduleId,
                    EffEndDate = effEndDate ?? default(DateOnly),
                    Outage = outage
                };
                return schedule;
            }

            return null;
    }

    public async Task UpdateScheduleFields(int scheduleId, DateOnly effStartDate, DateOnly? effEndDate, bool outage)
    {
        await using var command = _dataSource.CreateCommand(@"UPDATE schedules
            SET effective_start_date = $1, effective_end_date = $2, outage = $3
            WHERE schedule_id = $4;");

        command.Parameters.AddWithValue(effStartDate);
        command.Parameters.AddWithValue(effEndDate.HasValue ? (object)effEndDate.Value : DBNull.Value);
        command.Parameters.AddWithValue(outage);
        command.Parameters.AddWithValue(scheduleId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task ReplaceTimeRanges(int scheduleId, List<(DayOfWeek day, TimeOnly begin, TimeOnly end)> ranges)
    {
        await using var deleteCommand = _dataSource.CreateCommand(
            "DELETE FROM day_time_range WHERE schedule_id = $1;");
        deleteCommand.Parameters.AddWithValue(scheduleId);
        await deleteCommand.ExecuteNonQueryAsync();

        foreach (var (day, begin, end) in ranges)
        {
            await using var insertCommand = _dataSource.CreateCommand(@"INSERT INTO day_time_range
                (schedule_id, day_of_week, start_time, end_time)
                VALUES ($1, $2, $3, $4);");
            insertCommand.Parameters.AddWithValue(scheduleId);
            insertCommand.Parameters.AddWithValue((int)day);
            insertCommand.Parameters.AddWithValue(begin);
            insertCommand.Parameters.AddWithValue(end);
            await insertCommand.ExecuteNonQueryAsync();
        }
    }

    public async Task DeactivateSchedule(int scheduleId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        await using var command = _dataSource.CreateCommand(@"UPDATE schedules
            SET effective_end_date = $1
            WHERE schedule_id = $2;");
        command.Parameters.AddWithValue(today);
        command.Parameters.AddWithValue(scheduleId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> CreateSchedule(int techId, int salonId, DateOnly effStartDate, DateOnly? effEndDate, bool outage, List<(DayOfWeek day, TimeOnly begin, TimeOnly end)> ranges)
    {
        await using var command = _dataSource.CreateCommand(@"
            INSERT INTO schedules (account_id, salon_id, release_schedule, effective_start_date, effective_end_date, frequency, outage)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            RETURNING schedule_id;");
        command.Parameters.AddWithValue(techId);
        command.Parameters.AddWithValue(salonId);
        command.Parameters.AddWithValue(7);
        command.Parameters.AddWithValue(effStartDate);
        command.Parameters.AddWithValue(effEndDate.HasValue ? (object)effEndDate.Value : DBNull.Value);
        command.Parameters.AddWithValue(1);
        command.Parameters.AddWithValue(outage);
        var newScheduleId = (int)(await command.ExecuteScalarAsync())!;

        foreach (var (day, begin, end) in ranges)
        {
            await using var insert = _dataSource.CreateCommand(@"
                INSERT INTO day_time_range (schedule_id, day_of_week, start_time, end_time)
                VALUES ($1, $2, $3, $4);");
            insert.Parameters.AddWithValue(newScheduleId);
            insert.Parameters.AddWithValue((int)day);
            insert.Parameters.AddWithValue(begin);
            insert.Parameters.AddWithValue(end);
            await insert.ExecuteNonQueryAsync();
        }

        return newScheduleId;
    }
}
