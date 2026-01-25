using Microsoft.Extensions.Configuration;

public static class DiscordConfig
{
    public static string Token { get; private set; } = null!;
    public static ulong GuildId { get; private set; }
    public static ulong ChannelId { get; private set; }
    public static ulong MessageId { get; private set; }
    public static ulong BotId { get; private set; }

    public static void Initialize(IConfiguration config)
    {
        Token = config["DISCORD_TOKEN"]
            ?? config["DISCORD:TOKEN"]
            ?? throw new InvalidOperationException("Discord token not configured");

        GuildId = ulong.Parse(
            config["GUILD_ID"]
            ?? config["DISCORD:GUILD_ID"]
            ?? throw new InvalidOperationException("Guild ID not configured"));

        ChannelId = ulong.Parse(
            config["CHANNEL_ID"]
            ?? config["DISCORD:CHANNEL_ID"]
            ?? throw new InvalidOperationException("Channel ID not configured"));

        MessageId = ulong.Parse(
            config["MESSAGE_ID"]
            ?? config["DISCORD:MESSAGE_ID"]
            ?? throw new InvalidOperationException("Message ID not configured"));

        BotId = ulong.Parse(
            config["BOT_ID"]
            ?? config["DISCORD:BOT_ID"]
            ?? throw new InvalidOperationException("Bot ID not configured"));
    }
}