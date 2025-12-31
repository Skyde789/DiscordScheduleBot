using FFDiscordBot;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using Microsoft.Extensions.Configuration;


var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

string token =
    config["DISCORD_TOKEN"]
    ?? config["Discord:Token"]
    ?? throw new InvalidOperationException("Discord token not configured"); ulong botID = ulong.Parse(config["Discord:BotId"] ?? throw new InvalidOperationException("Discord botId not configured"));

GatewayClient client = new(
    new BotToken(token),
    new GatewayClientConfiguration
    {
        Logger = new ConsoleLogger(),
        Intents = GatewayIntents.GuildMessages |
                  GatewayIntents.MessageContent |
                  GatewayIntents.GuildMessageReactions,
    }
);

BotData.Initialize();

ApplicationCommandService<ApplicationCommandContext> appService = new();
appService.AddModule<RaidPlannerModule>();

ComponentInteractionService<ButtonInteractionContext> buttonService = new();
buttonService.AddModule<ButtonModule>();

ComponentInteractionService<StringMenuInteractionContext> stringMenuService = new();
stringMenuService.AddModule<StringMenuModule>();
/*
ComponentInteractionService<UserMenuInteractionContext> userMenuService = new();
userMenuService.AddModule<TestUserMenuModule>();*/

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

// Register the commands so that you can use them in the Discord client
await appService.RegisterCommandsAsync(client.Rest, client.Id);


await client.StartAsync();
await Task.Delay(-1);

