using NetCord;
using Npgsql;
public class PollPeriod
{
    public DayOfWeek Start { get; set; } = DayOfWeek.Tuesday;
    public DayOfWeek End { get; set; } = DayOfWeek.Monday;

    public PollPeriod() { }
    public PollPeriod(DayOfWeek start, DayOfWeek end)
    {
        Start = start;
        End = end;
    }
}

public class GuildSettings
{
    public int Version { get; set; } = 1;

    public List<DayOfWeek> SelectedDays { get; set; } = 
        new List<DayOfWeek>   { DayOfWeek.Monday, 
                                DayOfWeek.Tuesday, 
                                DayOfWeek.Wednesday,
                                DayOfWeek.Thursday, 
                                DayOfWeek.Friday, 
                                DayOfWeek.Saturday, 
                                DayOfWeek.Sunday };

    public PollPeriod PollPeriod { get; set; } = new();
}

public class Database
{
    private readonly string _connString;

    // The singleton instance
    public static Database Instance { get; } = new Database();

    public Database()
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? throw new InvalidOperationException("DATABASE_URL not set");
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        _connString =
            $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={uri.AbsolutePath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
    }

    public void InitializeGuild(ulong guildId)
    {
        using var conn = new NpgsqlConnection(_connString);
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO guild_settings (guild_id, selected_days, poll_start_day, poll_end_day, version)
              VALUES (@id, @days, @start, @end, @version)
              ON CONFLICT (guild_id) DO NOTHING;", conn);

        cmd.Parameters.AddWithValue("id", (long)guildId);
        cmd.Parameters.AddWithValue("days", new int[] { 0, 1, 2, 3, 4, 5, 6 });  // All days
        cmd.Parameters.AddWithValue("start", (int)DayOfWeek.Tuesday);
        cmd.Parameters.AddWithValue("end", (int)DayOfWeek.Monday);
        cmd.Parameters.AddWithValue("version", 1);

        cmd.ExecuteNonQuery();
    }

    public GuildSettings? GetGuildSettings(ulong guildId)
    {
        using var conn = new NpgsqlConnection(_connString);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT selected_days, poll_start_day, poll_end_day, version FROM guild_settings WHERE guild_id=@id", conn);
        cmd.Parameters.AddWithValue("id", (long)guildId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var days = reader.GetFieldValue<int[]>(0).ToList(); // directly get int array
        List<DayOfWeek> selectedDays = new List<DayOfWeek>();

        foreach (var day in days) 
        {
            selectedDays.Add((DayOfWeek)day);
        }

        var start = (DayOfWeek)reader.GetInt32(1);
        var end = (DayOfWeek)reader.GetInt32(2);
        var version = reader.GetInt32(3);

        return new GuildSettings
        {
            SelectedDays = selectedDays,
            PollPeriod = new PollPeriod(start, end),
            Version = version
        };
    }

    public void ModifyGuildSettings(ulong guildId, GuildSettings settings)
    {
        using var conn = new NpgsqlConnection(_connString);
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE guild_settings
              SET selected_days=@days, poll_start_day=@start, poll_end_day=@end, version=@version
              WHERE guild_id=@id", conn);

        cmd.Parameters.AddWithValue("id", (long)guildId);
        cmd.Parameters.AddWithValue("days", settings.SelectedDays.ToArray()); // int array
        cmd.Parameters.AddWithValue("start", (int)settings.PollPeriod.Start);
        cmd.Parameters.AddWithValue("end", (int)settings.PollPeriod.End);
        cmd.Parameters.AddWithValue("version", settings.Version);

        cmd.ExecuteNonQuery();
    }
}
