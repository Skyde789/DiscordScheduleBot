using System.Text.Json;
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

public class BotData
{
    public const int CURRENT_VERSION = 1;
    const string fileName = "data.json";

    private static readonly object _lock = new();
    static string FilePath => Path.Combine(AppContext.BaseDirectory, fileName);
    public Dictionary<ulong, GuildSettings> GuildSchedules { get; set; } = new();
    public static BotData? Current { get; private set; }
    public static void Initialize() => Current = LoadData();

    public List<DayOfWeek>? GetSelectedDays(ulong guildID)
        => Current!.GuildSchedules.TryGetValue(guildID, out var settings)
            ? settings.SelectedDays
            : null;
    public GuildSettings? GetGuildSettings(ulong guildID)
        => Current!.GuildSchedules.TryGetValue(guildID, out var settings)
            ? settings
            : null;
    public PollPeriod GetPollingPeriod(ulong guildID) => Current!.GuildSchedules.TryGetValue(guildID, out var settings)
            ? settings.PollPeriod
            : new PollPeriod(); 

    public void InitializeGuild(ulong guildID)
    {
        if (!Current!.GuildSchedules.TryGetValue(guildID, out var settings) ||
            settings.Version < CURRENT_VERSION)
        {
            settings = new GuildSettings
            {
                Version = CURRENT_VERSION,
                SelectedDays = new List<DayOfWeek>{ DayOfWeek.Monday,
                                                    DayOfWeek.Tuesday,
                                                    DayOfWeek.Wednesday,
                                                    DayOfWeek.Thursday,
                                                    DayOfWeek.Friday,
                                                    DayOfWeek.Saturday,
                                                    DayOfWeek.Sunday },
                PollPeriod = new PollPeriod()
            };
            Current.GuildSchedules[guildID] = settings;
            SaveData();
            Console.WriteLine($"Initialized or upgraded guild {guildID} to version {CURRENT_VERSION}");
        }
    }

    public void ModifySelectedDays(ulong guildID, List<DayOfWeek> selectedDays)
    {
        if (!Current!.GuildSchedules.TryGetValue(guildID, out var settings))
            settings = new GuildSettings();

        settings.SelectedDays = selectedDays;
        Current.GuildSchedules[guildID] = settings;

        SaveData();
    }

    public void ModifyPollingPeriod(ulong guildID, PollPeriod period)
    {
        if (!Current!.GuildSchedules.TryGetValue(guildID, out var settings))
            settings = new GuildSettings();

        settings.PollPeriod = period;

        if(!settings.SelectedDays.Contains(period.Start))
            settings.SelectedDays.Add(period.Start);

        if(!settings.SelectedDays.Contains(period.End))
            settings.SelectedDays.Add(period.End);

        Current.GuildSchedules[guildID] = settings;

        SaveData();
    }

    public static void SaveData()
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }
    }

    public static BotData LoadData()
    {
        if (!File.Exists(FilePath))
            return new BotData();

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<BotData>(json) ?? new BotData();
    }
}
