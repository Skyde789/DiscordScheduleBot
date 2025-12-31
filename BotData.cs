
using System.Text.Json;
public class BotData
{
    const string fileName = "data.json";
    private static readonly object _lock = new();
    static string FilePath => Path.Combine(AppContext.BaseDirectory, fileName);

    public Dictionary<ulong, List<DayOfWeek>> GuildSchedules { get; set; } = new();

    public static BotData? Current { get; private set; }

    public static void Initialize() => Current = LoadData();

    public List<DayOfWeek>? GetScheduleForGuild(ulong guildID)
    {
        if (Current!.GuildSchedules.TryGetValue(guildID, out var settings))
            return settings;
    
        return null;
    }

    public void ModifyScheduleForGuild(ulong guildID, List<DayOfWeek> schedule)
    {
        Current!.GuildSchedules[guildID] = schedule;

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