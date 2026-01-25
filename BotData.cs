using NetCord.Gateway;
using NetCord.Rest;
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

public class GuildRegistry
{
    public int Version => BotData.CURRENT_VERSION;
    public Dictionary<ulong, ulong> GuildLinks { get; set; } = new();
}

public class BotData
{
    public const int CURRENT_VERSION = 2;
    const string fileName = "data.json";

    private static readonly object _lock = new();

    public Dictionary<ulong, GuildSettings> GuildSchedules { get; set; } = new();
    public GuildRegistry GuildRegistry { get; set; } = new();

    public static BotData? Current { get; private set; }

    private static GatewayClient? client;

    public static void Initialize(string? discordJson, GatewayClient _client)
    {
        client = _client;
        Current = new BotData(); 
        Current.GuildRegistry = Current.LoadGuildLinks(discordJson);
        Console.WriteLine("Successfully loaded registry from discord message");
        foreach (var item in Current.GuildRegistry.GuildLinks)
        {
            Console.WriteLine("Guild found: " + item);
        }
    }

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

    public async Task InitializeGuild(ulong guildID)
    {
        Console.WriteLine("");
        if (!Current!.GuildSchedules.TryGetValue(guildID, out var settings) || settings.Version < CURRENT_VERSION)
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
            await SaveGuildSettings(guildID);
            Console.WriteLine($"Initialized or upgraded guild {guildID} to version {CURRENT_VERSION}");
        }
    }

    public async Task ModifySelectedDays(ulong guildID, List<DayOfWeek> selectedDays)
    {
        if (!Current!.GuildSchedules.TryGetValue(guildID, out var settings))
            settings = new GuildSettings();

        settings.SelectedDays = selectedDays;
        Current.GuildSchedules[guildID] = settings;

        await SaveGuildSettings(guildID);
    }

    public async Task ModifyPollingPeriod(ulong guildID, PollPeriod period)
    {
        if (!Current!.GuildSchedules.TryGetValue(guildID, out var settings))
            settings = new GuildSettings();

        settings.PollPeriod = period;

        if(!settings.SelectedDays.Contains(period.Start))
            settings.SelectedDays.Add(period.Start);

        if(!settings.SelectedDays.Contains(period.End))
            settings.SelectedDays.Add(period.End);

        Current.GuildSchedules[guildID] = settings;

        await SaveGuildSettings(guildID);
    }

    public static async Task SaveGuildRegistry()
    {
        string json;
        lock (_lock)
        {
            json = JsonSerializer.Serialize(Current?.GuildRegistry, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        
        await client!.Rest.ModifyMessageAsync(DiscordConfig.ChannelId, DiscordConfig.MessageId, options =>
        {
            options.Content = json;
        });
        Console.WriteLine("Successfully saved guild registry\n");
    }

    public static async Task<RestMessage> HandleMissingSettings(ulong guildId)
    {
        Console.WriteLine("Guild settings not found, creating new message");
        RestMessage message;

        lock (_lock)
        {
            message = client!.Rest.SendMessageAsync(DiscordConfig.ChannelId, "placeholder for: " + guildId).GetAwaiter().GetResult();
            Current!.GuildRegistry.GuildLinks[guildId] = message.Id;
        }

        await SaveGuildRegistry();
        Console.WriteLine("Guild settings created.\n");

        return message;
    }

    public static async Task SaveGuildSettings(ulong guildId)
    {
        string json;
        lock (_lock)
        {
            json = JsonSerializer.Serialize(Current!.GuildSchedules[guildId], new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        try
        {
            await client!.Rest.ModifyMessageAsync(DiscordConfig.ChannelId, Current.GuildRegistry.GuildLinks[guildId], options =>
            {
                options.Content = json;
            });
        }
        catch
        {
            await HandleMissingSettings(guildId);
            await client!.Rest.ModifyMessageAsync(DiscordConfig.ChannelId, Current.GuildRegistry.GuildLinks[guildId], options =>
            {
                options.Content = json;
            });
            Console.WriteLine("Recovered missing guild message for " + guildId);
        }

        Console.WriteLine("Successfully saved guild settings for: " + guildId + "\n");
    }
    
    public GuildRegistry LoadGuildLinks(string? messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent) && IsValidJson(messageContent))
            return new GuildRegistry();

        return JsonSerializer.Deserialize<GuildRegistry>(messageContent) ?? new GuildRegistry();
    }

    public void LoadGuildSettingsFromMessage(string? messageContent, ulong guildId)
    {
        GuildSettings settings = new GuildSettings();

        if (!string.IsNullOrWhiteSpace(messageContent) && IsValidJson(messageContent))
            settings = JsonSerializer.Deserialize<GuildSettings>(messageContent);

        GuildSchedules.Add(guildId, settings!);
    }

    private bool IsValidJson(string s)
    {
        try
        {
            using var doc = JsonDocument.Parse(s);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
