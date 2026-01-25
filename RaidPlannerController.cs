
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
        public static ButtonProperties PollingPeriodButton => new ButtonProperties(
            customId: "polling_period_selection",
            label: "Polling Period",
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
            style: ButtonStyle.Secondary
        );
        public static ButtonProperties NextWeekButton => new ButtonProperties(
            customId: "next_week_button",
            label: "Poll next week",
            style: ButtonStyle.Primary
        );

        public static async Task GeneratePoll(IInteractionContext Context, bool thisWeek)
        {
            ulong guildId;

            guildId = (ulong)GetGuildIdFromContext(Context)!;

            GuildSettings? schedule = BotData.Current!.GetGuildSettings(guildId);

            if (schedule == null || schedule.SelectedDays.Count == 0)
                throw new InvalidOperationException("No days selected for this guild.");

            List<DateTime> dates = DateGenerator.GenerateDates(schedule, thisWeek);

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
            List<DayOfWeek>? currentSelection = BotData.Current!.GetSelectedDays(guildID);

            var selectMenu = new StringMenuProperties(
                customId: "day_menu",
                options:
                [
                new StringMenuSelectOptionProperties("Monday",     "1").WithDefault(currentSelection?.Contains(DayOfWeek.Monday)   ?? true),
                new StringMenuSelectOptionProperties("Tuesday",    "2").WithDefault(currentSelection?.Contains(DayOfWeek.Tuesday)  ?? true),
                new StringMenuSelectOptionProperties("Wednesday",  "3").WithDefault(currentSelection?.Contains(DayOfWeek.Wednesday)?? true),
                new StringMenuSelectOptionProperties("Thursday",   "4").WithDefault(currentSelection?.Contains(DayOfWeek.Thursday) ?? true),
                new StringMenuSelectOptionProperties("Friday",     "5").WithDefault(currentSelection?.Contains(DayOfWeek.Friday)   ?? true),
                new StringMenuSelectOptionProperties("Saturday",   "6").WithDefault(currentSelection?.Contains(DayOfWeek.Saturday) ?? true),
                new StringMenuSelectOptionProperties("Sunday",     "0").WithDefault(currentSelection?.Contains(DayOfWeek.Sunday)   ?? true)
            ])
            {
                Placeholder = "Pick your options",
                MinValues = 1,  
                MaxValues = 7   
            };

            var actionRow = new ActionRowProperties([InterfaceButton, CloseButton]);
            var message = new InteractionMessageProperties
            {
                Content = "Select multiple options (1-7):",
                Components = [selectMenu, actionRow]
            };

            return message;
        }

        public static InteractionMessageProperties GeneratePollPeriodMessage(ulong guildID)
        {
            PollPeriod? pollPeriod = BotData.Current!.GetPollingPeriod(guildID);

            var selectMenu = new StringMenuProperties(
                customId: "polling_period_menu",
                options:
                [
                new StringMenuSelectOptionProperties("Monday",      "1"),
                new StringMenuSelectOptionProperties("Tuesday",     "2"),
                new StringMenuSelectOptionProperties("Wednesday",   "3"),
                new StringMenuSelectOptionProperties("Thursday",    "4"),
                new StringMenuSelectOptionProperties("Friday",      "5"),
                new StringMenuSelectOptionProperties("Saturday",    "6"),
                new StringMenuSelectOptionProperties("Sunday",      "0")
            ])
            {
                Placeholder = $"Current: {pollPeriod.Start.ToString()} - {pollPeriod.End.ToString()}",
                MinValues = 2,
                MaxValues = 2
            };

            var actionRow = new ActionRowProperties([InterfaceButton, CloseButton]);
            var message = new InteractionMessageProperties
            {
                Content = "Select the starting day first, then the end date:",
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
            var actionRow1 = new ActionRowProperties([NextWeekButton, ThisWeekButton]);
            var actionRow2 = new ActionRowProperties([DaySelectButton, PollingPeriodButton]);
            var actionRow3 = new ActionRowProperties([CloseButton, CleanUpButton]);

            var message = new InteractionMessageProperties
            {
                Content = $"What would you like to do?",
                Components = [actionRow1, actionRow2, actionRow3]
            };

            return message;
        }
        
        public static async Task Cleanup(dynamic Context)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

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

            await Context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties
            {
                Content = "Bot messages deleted!",
                Flags = MessageFlags.Ephemeral
            });
        }
    
        public static async Task HandleDaySelect(StringMenuInteractionContext Context)
        {
            ulong guildId = (ulong)GetGuildIdFromContext(Context)!;

            var selectedValues = Context.Interaction.Data.SelectedValues; 
            List<DayOfWeek> parsedDays = new List<DayOfWeek>();
            string result = "";

            for (int i = 0; i < selectedValues.Count; i++)
            {
                parsedDays.Add((DayOfWeek)int.Parse(selectedValues[i]));

                result += parsedDays[i] + "\n";
            }

            await BotData.Current!.ModifySelectedDays(guildId, parsedDays);

            var newMessage = GenerateSelectDaysMessage(guildId);

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.ModifyMessage(msg =>
                {
                    msg.WithContent($"Selection saved!\n{result}");
                    msg.Components = newMessage.Components;
                })
            );
        }

        public static async Task HandlePollingPeriodSelect(StringMenuInteractionContext Context)
        {
            ulong guildId = (ulong)GetGuildIdFromContext(Context)!;

            var selectedValues = Context.Interaction.Data.SelectedValues;

            if(selectedValues.Count == 0)
                throw new InvalidOperationException("Start/End date is null");

            DayOfWeek parsedStartDate = (DayOfWeek)int.Parse(selectedValues[0]);
            DayOfWeek parsedEndDate = (DayOfWeek)int.Parse(selectedValues[1]);

            PollPeriod period = new PollPeriod(parsedStartDate, parsedEndDate);

            string result = "New Period: " + parsedStartDate.ToString() + " - " + parsedEndDate.ToString();

            await BotData.Current!.ModifyPollingPeriod(guildId, period);

            var newMessage = GeneratePollPeriodMessage(guildId);

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
