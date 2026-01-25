using FFDiscordBot;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using System.Threading.Channels;


var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

DiscordConfig.Initialize(config);

GatewayClient client = new(
    new BotToken(DiscordConfig.Token),
    new GatewayClientConfiguration
    {
        Logger = new ConsoleLogger(),
        Intents = GatewayIntents.GuildMessages |
                  GatewayIntents.MessageContent |
                  GatewayIntents.GuildMessageReactions,
    }
);

ApplicationCommandService<ApplicationCommandContext> appService = new();
appService.AddModule<RaidPlannerModule>();

ComponentInteractionService<ButtonInteractionContext> buttonService = new();
buttonService.AddModule<ButtonModule>();

ComponentInteractionService<StringMenuInteractionContext> stringMenuService = new();
stringMenuService.AddModule<StringMenuModule>();
/*
ComponentInteractionService<UserMenuInteractionContext> userMenuService = new();
userMenuService.AddModule<TestUserMenuModule>();
*/

client.InteractionCreate += async interaction =>
{
    var result = await (interaction switch
    {
        ApplicationCommandInteraction app =>
            appService.ExecuteAsync(new ApplicationCommandContext(app, client)),

        ButtonInteraction button =>
            buttonService.ExecuteAsync(new ButtonInteractionContext(button, client)),

        StringMenuInteraction menu =>
            stringMenuService.ExecuteAsync(new StringMenuInteractionContext(menu, client)),
        /*
        UserMenuInteraction menu =>
            userMenuService.ExecuteAsync(new UserMenuInteractionContext(menu, client)),
        */
        _ => appService.ExecuteAsync(new ApplicationCommandContext((ApplicationCommandInteraction)interaction, client))
    });

    if (result is not IFailResult failResult)
        return;

    try
    {
        await interaction.SendResponseAsync(InteractionCallback.Message(failResult.Message));
    }
    catch { }
};



client.Ready += async _ =>
{

    var fetchedMessage = await client.Rest.GetMessageAsync(DiscordConfig.ChannelId, DiscordConfig.MessageId);

    /* // Setup from here:
    await client.Rest.SendMessageAsync(DiscordConfig.ChannelId, "{\r\n  \"Version\": 1,\r\n  \"GuildLinks\": {}\r\n}\r\n");  
     */

    /* // modify from here:
    await client.Rest.ModifyMessageAsync(DiscordConfig.ChannelId, DiscordConfig.MessageId, message =>
    {
        message.Content = "{\r\n  \"Version\": 1,\r\n  \"GuildLinks\": {}\r\n}\r\n";
    });
    */

    BotData.Initialize(fetchedMessage.Content, client);
    
    await foreach (var guild in client.Rest.GetCurrentUserGuildsAsync())
    {
        if (!BotData.Current!.GuildRegistry.GuildLinks.ContainsKey(guild.Id))
        {
            Console.WriteLine("New guild (" + guild.Id + ") detected.");
            var newMessage = await client.Rest.SendMessageAsync(DiscordConfig.ChannelId, "placeholder for: " + guild.Id);
            BotData.Current.GuildRegistry.GuildLinks.Add(guild.Id, newMessage.Id);
            Console.WriteLine("New guild (" + guild.Id + ") added to registry");
        }
    }

    await BotData.SaveGuildRegistry();

    foreach (KeyValuePair<ulong, ulong> pair in BotData.Current!.GuildRegistry.GuildLinks)
    {
        ulong guildId = pair.Key;
        ulong messageId = pair.Value;

        try
        {
            fetchedMessage = await client.Rest.GetMessageAsync(DiscordConfig.ChannelId, messageId);
            Console.WriteLine("Guild settings message found.");
        }
        catch
        {
            fetchedMessage = await BotData.HandleMissingSettings(guildId);
        }

        BotData.Current.LoadGuildSettingsFromMessage(fetchedMessage.Content, guildId);
        await BotData.Current.InitializeGuild(guildId);
    }
    
    await Task.CompletedTask; 
};


await appService.RegisterCommandsAsync(client.Rest, client.Id);

await client.StartAsync();
await Task.Delay(-1);

