using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;

namespace FFDiscordBot
{
    public class TestModule : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("cleanup", "Delete all messages sent by the bot in this channel.")]
        public async Task CleanupAsync()
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

        [SlashCommand("pong", "Pong!")]
        public async Task PongAsync()
        {
            // Build interaction response properties
            var message = new InteractionMessageProperties
            {
                Content = $"Pong back to you! <@{Context.User.Id}>",
            };

            ButtonProperties button = new ButtonProperties(
                         customId: "pong_button",
                         label: "Click me!",
                         style: ButtonStyle.Primary
                     );
            
            var actionRow = new ActionRowProperties([button]);


            // Add the row to the message’s component list
            message.Components = [actionRow];

            // Send the response
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(message)
            );
        }

        [SlashCommand("poll", "Poll!")]
        public async Task PollAsync()
        {
            // Build the poll question
            var question = new MessagePollMediaProperties().WithText("Raid days");

            // Build poll answers
            var answers = new[]
            {
                new MessagePollAnswerProperties(
                    new MessagePollMediaProperties().WithText("Monday")
                ),
                new MessagePollAnswerProperties(
                    new MessagePollMediaProperties().WithText("Tuesday")
                ),
                new MessagePollAnswerProperties(
                    new MessagePollMediaProperties().WithText("Wednesday")
                ),
                new MessagePollAnswerProperties(
                    new MessagePollMediaProperties().WithText("Thursday")
                ),
                new MessagePollAnswerProperties(
                    new MessagePollMediaProperties().WithText("Friday")
                ),
                new MessagePollAnswerProperties(
                    new MessagePollMediaProperties().WithText("Saturday")
                ),
                new MessagePollAnswerProperties(
                    new MessagePollMediaProperties().WithText("Sunday")
                )
            };

            // Create the poll
            var poll = new MessagePollProperties(question, answers)
                .WithAllowMultiselect(true)
                .WithDurationInHours(24); // poll open for 24 hours

            // Send the poll with the message
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties
                {
                    Poll = poll
                })
            );
        }

        [SlashCommand("menu", "A menu test")]
        public async Task MenuAsync()
        {
            // Build interaction response properties
            var message = new InteractionMessageProperties
            {
                Content = $"This is a menu!"
            };


            var selectMenu = new StringMenuProperties(customId: "menu_test", options:
            [
                new StringMenuSelectOptionProperties(
                    label: "Option 1",
                    value: "opt1"
                ).WithDescription("First option!"),
                new StringMenuSelectOptionProperties(
                    label: "Option 2",
                    value: "opt2"
                ).WithDescription("Second option!")
            ])
            {
                Placeholder = "Choose an option"
            };

            message.Components = [selectMenu];

            // Send the response
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(message)
            );
        }

        [SlashCommand("usermenu", "A menu test")]
        public async Task UserMenuAsync()
        {
            // Build interaction response properties
            var message = new InteractionMessageProperties
            {
                Content = $"This is a user menu!"
            };


            var selectMenu = new UserMenuProperties(customId: "usermenu_test")
            {
                Placeholder = "Choose an option"
            };

            message.Components = [selectMenu];

            // Send the response
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(message)
            );
        }

        [SlashCommand("multi", "Select multiple options")]
        public async Task MultiSelectMenuAsync()
        {
            var message = new InteractionMessageProperties
            {
                Content = "Select multiple options (1-7):"
            };

            var selectMenu = new StringMenuProperties(
                customId: "multi_menu",
                options: 
                [
                new StringMenuSelectOptionProperties("Mondays",     "1"),
                new StringMenuSelectOptionProperties("Tuesdays",    "2"),
                new StringMenuSelectOptionProperties("Wednesdays",  "3"),
                new StringMenuSelectOptionProperties("Thursdays",   "4"),
                new StringMenuSelectOptionProperties("Fridays",     "5"),
                new StringMenuSelectOptionProperties("Saturdays",   "6"),
                new StringMenuSelectOptionProperties("Sundays",     "7")
            ])
            {
                Placeholder = "Pick your options",
                MinValues = 0,  // at least 1 option must be selected
                MaxValues = 7   // at most 7 options can be selected
            };

            message.Components = [selectMenu];

            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
        }


        [UserCommand("ID")]
        public static string Id(User user) => user.Id.ToString();

        [MessageCommand("Timestamp")]
        public static string Timestamp(RestMessage message) => message.CreatedAt.ToString();
    }

    public class TestButtonModule : ComponentInteractionModule<ButtonInteractionContext>
    {
        [ComponentInteraction("pong_button")]
        public async Task PongButton()
        {
            // Update the original message and the button
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.ModifyMessage(message =>
                {
                    // Change the message content
                    message.WithContent("Ping! Button updated!");

                    ButtonProperties button = new ButtonProperties(
                        customId: "ping_button",
                        label: "Click me!",
                        style: ButtonStyle.Primary
                    );

                    var actionRow = new ActionRowProperties([button]);


                    // Replace the old button with a new one
                    message.Components = [actionRow];
                })
            );
        }
        [ComponentInteraction("ping_button")]
        public async Task PingButton()
        {
            // Update the original message and the button
            await Context.Interaction.SendResponseAsync(InteractionCallback.ModifyMessage(message =>
                {
                    // Change the message content
                    message.WithContent("Pong! Button updated!");

                    ButtonProperties button = new ButtonProperties(
                        customId: "pong_button",
                        label: "Click me!",
                        style: ButtonStyle.Primary
                    );

                    var actionRow = new ActionRowProperties([button]);

                    // Replace the old button with a new one
                    message.Components = [actionRow];
                })
            );
            await Context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties
            {
                Content = "Hello world! (ephemeral)",
                Flags = MessageFlags.Ephemeral
            });
        }
    }

    public class TestStringMenuModule : ComponentInteractionModule<StringMenuInteractionContext>
    {
        
        [ComponentInteraction("menu_test")]
        public string Menu() => $"You selected: {string.Join(", ", Context.SelectedValues)}";

        [ComponentInteraction("multi_menu")]
        public async Task HandleMultiMenu()
        {
            var selectedValues = Context.Interaction.Data.SelectedValues; // List<string>
            DayOfWeek[] asd = new DayOfWeek[selectedValues.Count];
            string result = "";

            for (int i = 0; i < selectedValues.Count; i++)
            {
                asd[i] = (DayOfWeek)int.Parse(selectedValues[i]);

                result += asd[i] + "\n";
            }

            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties
                {
                    Content = $"You selected:\n{result}",
                    Flags = MessageFlags.Ephemeral
                })
            );
        }
    }


    public class TestUserMenuModule : ComponentInteractionModule<UserMenuInteractionContext>
    {
        [ComponentInteraction("usermenu_test")]
        public string Menu() => $"You selected: {string.Join(", ", Context.SelectedValues)}";
    }

}

