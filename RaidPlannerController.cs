
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;

namespace FFDiscordBot
{
 
    public static class RaidPlannerController
    {
        public static ButtonProperties DaySelectButton => new ButtonProperties(
            customId: "day_selection",
            label: "Day Selection",
            style: ButtonStyle.Secondary
        );
        public static ButtonProperties CleanUpButton => new ButtonProperties(
            customId: "cleanup_button",
            label: "Clean Messages",
            style: ButtonStyle.Secondary
        );
        public static ButtonProperties InterfaceButton => new ButtonProperties(
            customId: "interface_button",
            label: "Back",
            style: ButtonStyle.Primary
        );
        public static ButtonProperties CloseButton => new ButtonProperties(
            customId: "close_button",
            label: "Close",
            style: ButtonStyle.Danger
        );
        public static ButtonProperties ThisWeekButton => new ButtonProperties(
            customId: "this_week_button",
            label: "Poll this week",
            style: ButtonStyle.Primary
        );
        public static ButtonProperties NextWeekButton => new ButtonProperties(
            customId: "next_week_button",
            label: "Poll next week",
            style: ButtonStyle.Primary
        );

        public static async Task GeneratePoll(IInteractionContext Context, int weeksFromNow)
        {
            ulong guildId;

            guildId = (ulong)GetGuildIdFromContext(Context);

            List<DayOfWeek>? schedule = BotData.Current!.GetScheduleForGuild(guildId);

            if (schedule == null || schedule.Count == 0)
                throw new InvalidOperationException("No days selected for this guild.");

            List<DateTime> dates = DateGenerator.GenerateWeeklyPollDates(schedule, weeksFromNow);

            var message = new MessagePollMediaProperties().WithText("Raid days");

            List<MessagePollAnswerProperties> answers = new List<MessagePollAnswerProperties>();

            foreach (DateTime date in dates)
            {
                answers.Add(new MessagePollAnswerProperties(
                            new MessagePollMediaProperties().WithText($"{date.Day}.{date.Month}. {date.DayOfWeek.ToString()}")
                ));
            }

            var poll = new MessagePollProperties(message, answers.ToArray())
                .WithAllowMultiselect(true)
                .WithDurationInHours(24);

            if(poll.Answers.Count() == 0)
            {
                await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties
                {
                    Content = "No scheduled days available for this week!",
                    Flags = MessageFlags.Ephemeral
                }));
                return;
            }
            
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties
                {
                    Poll = poll
                })
            );

            if(Context is ButtonInteractionContext bcc)
            {
                if (bcc.Interaction.Message is not null)
                {
                    await bcc.Interaction.Message.DeleteAsync();
                }
            }
        }

        public static InteractionMessageProperties GenerateSelectDaysMessage(ulong guildID)
        {
            List<DayOfWeek>? currentSelection = BotData.Current!.GetScheduleForGuild(guildID);

            var selectMenu = new StringMenuProperties(
                customId: "day_menu",
                options:
                [
                new StringMenuSelectOptionProperties("Monday",     "1").WithDefault(currentSelection?.Contains(DayOfWeek.Monday)   ?? false),
                new StringMenuSelectOptionProperties("Tuesday",    "2").WithDefault(currentSelection?.Contains(DayOfWeek.Tuesday)  ?? false),
                new StringMenuSelectOptionProperties("Wednesday",  "3").WithDefault(currentSelection?.Contains(DayOfWeek.Wednesday)?? false),
                new StringMenuSelectOptionProperties("Thursday",   "4").WithDefault(currentSelection?.Contains(DayOfWeek.Thursday) ?? false),
                new StringMenuSelectOptionProperties("Friday",     "5").WithDefault(currentSelection?.Contains(DayOfWeek.Friday)   ?? false),
                new StringMenuSelectOptionProperties("Saturday",   "6").WithDefault(currentSelection?.Contains(DayOfWeek.Saturday) ?? false),
                new StringMenuSelectOptionProperties("Sunday",     "0").WithDefault(currentSelection?.Contains(DayOfWeek.Sunday)   ?? false)
            ])
            {
                Placeholder = "Pick your options",
                MinValues = 0,  // at least 1 option must be selected
                MaxValues = 7   // at most 7 options can be selected
            };

            var actionRow = new ActionRowProperties([InterfaceButton, CloseButton]);
            var message = new InteractionMessageProperties
            {
                Content = "Select multiple options (1-7):",
                Components = [selectMenu, actionRow]
            };

            return message;
        }

        public static ulong? GetGuildIdFromContext(IInteractionContext Context)
        {
            if (Context.Interaction.GuildId != null)
                return Context.Interaction.GuildId;
            else if (Context.Interaction.Channel is IGuildChannel guildChannel)
                return guildChannel.GuildId;
            
            return null;
        }
        
        public static InteractionMessageProperties GenerateInterface()
        {
            var actionRow1 = new ActionRowProperties([ThisWeekButton, NextWeekButton]);
            var actionRow2 = new ActionRowProperties([DaySelectButton, CleanUpButton]);
            var actionRow3 = new ActionRowProperties([CloseButton]);

            var message = new InteractionMessageProperties
            {
                Content = $"What would you like to do?",
                Components = [actionRow1, actionRow2, actionRow3]
            };

            return message;
        }
        
        public static async Task Cleanup(dynamic Context)
        {
            // Defer the response to give time for processing
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

            // Get the current channel
            var channel = Context.Interaction.Channel;

            if (channel is not TextChannel textChannel)
            {
                await Context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties
                {
                    Content = "This command can only be used in text channels.",
                    Flags = MessageFlags.Ephemeral
                });
                return;
            }

            int deletedCount = 0;

            await foreach (var message in textChannel.GetMessagesAsync(new PaginationProperties<ulong> { BatchSize = 50 }))
            {
                if (message.Author.Id != Context.Client.Id)
                    continue;

                await message.DeleteAsync();
                deletedCount++;

                await Task.Delay(500);
            }

            // Send ephemeral feedback
            await Context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties
            {
                Content = "Bot messages deleted!",
                Flags = MessageFlags.Ephemeral
            });
        }
    
        public static async Task HandleDaySelect(StringMenuInteractionContext Context)
        {
            ulong guildId = (ulong)GetGuildIdFromContext(Context);

            var selectedValues = Context.Interaction.Data.SelectedValues; // List<string>
            List<DayOfWeek> parsedDays = new List<DayOfWeek>();
            string result = "";

            for (int i = 0; i < selectedValues.Count; i++)
            {
                parsedDays.Add((DayOfWeek)int.Parse(selectedValues[i]));

                result += parsedDays[i] + "\n";
            }

            // Disable the menu by rebuilding the component rows
            var newActionRows = Context.Interaction.Message!.Components
                .OfType<ActionRowProperties>() // cast to ActionRowProperties
                .Select(row =>
                {
                    var newRow = new ActionRowProperties(row.Components.Select(c =>
                    {
                        if (c is StringMenuProperties menu && menu.CustomId == "day_menu")
                            menu.Disabled = true;
                        return c;
                    }).ToArray()
                    );
                    return newRow;
                }).ToArray();

            BotData.Current!.ModifyScheduleForGuild(guildId, parsedDays);

            var newMessage = GenerateSelectDaysMessage(guildId);

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.ModifyMessage(msg =>
                {
                    msg.WithContent($"Selection saved!\n{result}");
                    msg.Components = newMessage.Components;
                })
            );
        }
    }

}
