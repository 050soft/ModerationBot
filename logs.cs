using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace Moderation_Bot.commands
{
    public class LogsCommands : BaseCommandModule
    {
        // Store log channel IDs per guild: (commandLog, messageLog, actionLog)
        public static ConcurrentDictionary<ulong, (ulong commandLog, ulong messageLog, ulong actionLog)> LogChannels
            = new ConcurrentDictionary<ulong, (ulong, ulong, ulong)>();

        [Command("logs")]
        [Description("Set up a logging system for the server.")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task Logs(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();

            var embed = new DiscordEmbedBuilder
            {
                Title = "Logs Setup",
                Description = "Do you already have channels for logs?",
                Color = DiscordColor.Blurple
            };

            var yesButton = new DiscordButtonComponent(ButtonStyle.Success, "logs_yes", "Yes");
            var noButton = new DiscordButtonComponent(ButtonStyle.Danger, "logs_no", "No");

            var messageBuilder = new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(yesButton, noButton);

            var sentMessage = await ctx.Channel.SendMessageAsync(messageBuilder);

            var buttonResult = await interactivity.WaitForButtonAsync(sentMessage, ctx.User, TimeSpan.FromSeconds(30));

            if (buttonResult.TimedOut)
            {
                var timeoutBuilder = new DiscordMessageBuilder()
                    .WithContent("Timed out waiting for response.")
                    .WithEmbed(null);
                await sentMessage.ModifyAsync(timeoutBuilder);
                return;
            }

            // ACKNOWLEDGE THE INTERACTION TO PREVENT "INTERACTION FAILED"
            await buttonResult.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredMessageUpdate);

            var buttonId = buttonResult.Result.Id;

            if (buttonId == "logs_no")
            {
                // Create three private log channels
                var adminRole = ctx.Guild.Roles.Values.FirstOrDefault(r => r.Permissions.HasPermission(Permissions.Administrator));
                var everyoneRole = ctx.Guild.EveryoneRole;

                var commandLog = await ctx.Guild.CreateChannelAsync("command-logs", ChannelType.Text, null, "Command logs created by bot");
                var messageLog = await ctx.Guild.CreateChannelAsync("message-logs", ChannelType.Text, null, "Message logs created by bot");
                var actionLog = await ctx.Guild.CreateChannelAsync("action-logs", ChannelType.Text, null, "Action logs created by bot");

                // Set permissions to make the channels private
                if (adminRole != null)
                {
                    await commandLog.AddOverwriteAsync(adminRole, Permissions.AccessChannels | Permissions.SendMessages | Permissions.ReadMessageHistory);
                    await messageLog.AddOverwriteAsync(adminRole, Permissions.AccessChannels | Permissions.SendMessages | Permissions.ReadMessageHistory);
                    await actionLog.AddOverwriteAsync(adminRole, Permissions.AccessChannels | Permissions.SendMessages | Permissions.ReadMessageHistory);
                }

                await commandLog.AddOverwriteAsync(everyoneRole, Permissions.None, Permissions.AccessChannels);
                await messageLog.AddOverwriteAsync(everyoneRole, Permissions.None, Permissions.AccessChannels);
                await actionLog.AddOverwriteAsync(everyoneRole, Permissions.None, Permissions.AccessChannels);

                LogChannels[ctx.Guild.Id] = (commandLog.Id, messageLog.Id, actionLog.Id);

                var createdBuilder = new DiscordMessageBuilder()
                    .WithContent(
                        $"Log channels have been created:\n- {commandLog.Mention} (Command Logs)\n- {messageLog.Mention} (Message Logs)\n- {actionLog.Mention} (Action Logs)"
                    )
                    .WithEmbed(null);
                await sentMessage.ModifyAsync(createdBuilder);
            }
            else if (buttonId == "logs_yes")
            {
                var promptBuilder = new DiscordMessageBuilder()
                    .WithContent("Please mention the channels in the following order (all in one message):\n1. Command Log\n2. Message Log\n3. Action Log")
                    .WithEmbed(null);
                await sentMessage.ModifyAsync(promptBuilder);

                var channelResponse = await interactivity.WaitForMessageAsync(
                    x => x.Author.Id == ctx.User.Id && x.MentionedChannels.Count == 3,
                    TimeSpan.FromSeconds(60)
                );

                if (!channelResponse.TimedOut)
                {
                    var mentioned = channelResponse.Result.MentionedChannels.ToList();
                    LogChannels[ctx.Guild.Id] = (mentioned[0].Id, mentioned[1].Id, mentioned[2].Id);
                    await ctx.Channel.SendMessageAsync($"Log channels have been assigned:\n- {mentioned[0].Mention} (Command Logs)\n- {mentioned[1].Mention} (Message Logs)\n- {mentioned[2].Mention} (Action Logs)");
                }
                else
                {
                    await ctx.Channel.SendMessageAsync("Timed out waiting for channel mentions.");
                }
            }
        }
    }
}

// Example setup for DiscordClient and Interactivity registration
public class BotSetup
{
    public static DiscordClient InitializeDiscordClient()
    {
        var discord = new DiscordClient(new DiscordConfiguration
        {
            // your config here
        });

        // Register Interactivity ONCE here:
        discord.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(2) // or your preferred timeout
        });

        return discord;
    }
}