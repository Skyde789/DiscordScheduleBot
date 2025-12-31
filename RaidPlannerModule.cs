using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;

namespace FFDiscordBot
{
    public class RaidPlannerModule : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("interface", "Clickable interface!")]
        public async Task Interface()
        {
            var message = RaidPlannerController.GenerateInterface();

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(message)
            );
        }

        [SlashCommand("cleanup", "Delete all messages sent by the bot in this channel.")]
        public async Task Cleanup()
        {
            await RaidPlannerController.Cleanup(Context);
        }

        [SlashCommand("thisweek", "Poll for this week using selected days.")]
        public async Task PollThisWeek()
        {
           await RaidPlannerController.GeneratePoll(Context, 0);

        }

        [SlashCommand("nextweek", "Poll for the next week using selected days.")]
        public async Task PollNextWeek()
        {
            await RaidPlannerController.GeneratePoll(Context, 1);
        }

        [SlashCommand("dayselect", "Select days for polling")]
        public async Task DaySelectMenu()
        {
            var message = RaidPlannerController.GenerateSelectDaysMessage((ulong)Context.Interaction.GuildId);

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
        }

    }

    public class ButtonModule : ComponentInteractionModule<ButtonInteractionContext>
    {
        [ComponentInteraction("day_selection")]
        public async Task DaySelectButton()
        {
            ulong guildId = (ulong)RaidPlannerController.GetGuildIdFromContext(Context);

            var message = RaidPlannerController.GenerateSelectDaysMessage(guildId);

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.ModifyMessage(msg =>
                {
                    msg.Content = message.Content;
                    msg.Components = message.Components;
                })
            );
        }

        [ComponentInteraction("interface_button")]
        public async Task InterfaceButton()
        {
            var message = RaidPlannerController.GenerateInterface();

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.ModifyMessage(msg =>
                {
                    msg.Content = message.Content;
                    msg.Components = message.Components;
                })
            );
        }

        [ComponentInteraction("this_week_button")]
        public async Task ThisWeekButton()
        {
            await RaidPlannerController.GeneratePoll(Context, 0);
        }

        [ComponentInteraction("next_week_button")]
        public async Task NextWeekButton()
        {
            await RaidPlannerController.GeneratePoll(Context, 1);

        }

        [ComponentInteraction("cleanup_button")]
        public async Task CleanUpButton()
        {
            await RaidPlannerController.Cleanup(Context);
        }

        [ComponentInteraction("close_button")]
        public async Task CloseButton()
        {
            if (Context.Interaction.Message is not null)
            {
                await Context.Interaction.Message.DeleteAsync();
            }
        }
    }

    public class StringMenuModule : ComponentInteractionModule<StringMenuInteractionContext>
    {
        [ComponentInteraction("day_menu")]
        public async Task HandleMultiMenu()
        {
            await RaidPlannerController.HandleDaySelect(Context);
        }
    }
}

