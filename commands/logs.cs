using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
            var embed = new DiscordEmbedBuilder
            {
                Title = "Logs Setup",
                Description = "Do you already have channels for logs? Reply with `yes` or `no`.",
                Color = DiscordColor.Blurple
            };
            await ctx.RespondAsync(embed: embed);

            var interactivity = ctx.Client.GetInteractivity();
            var response = await interactivity.WaitForMessageAsync(
                x => x.Author.Id == ctx.User.Id && (x.Content.Equals("yes", StringComparison.OrdinalIgnoreCase) || x.Content.Equals("no", StringComparison.OrdinalIgnoreCase)),
                TimeSpan.FromSeconds(30)
            );

            if (!response.TimedOut)
            {
                if (response.Result.Content.Equals("no", StringComparison.OrdinalIgnoreCase))
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

                    await ctx.RespondAsync($"Log channels have been created:\n- {commandLog.Mention} (Command Logs)\n- {messageLog.Mention} (Message Logs)\n- {actionLog.Mention} (Action Logs)");
                }
                else if (response.Result.Content.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    var prompt = await ctx.RespondAsync("Please mention the channels in the following order (all in one message):\n1. Command Log\n2. Message Log\n3. Action Log");
                    var channelResponse = await interactivity.WaitForMessageAsync(
                        x => x.Author.Id == ctx.User.Id && x.MentionedChannels.Count == 3,
                        TimeSpan.FromSeconds(60)
                    );

                    if (!channelResponse.TimedOut)
                    {
                        var mentioned = channelResponse.Result.MentionedChannels.ToList();
                        LogChannels[ctx.Guild.Id] = (mentioned[0].Id, mentioned[1].Id, mentioned[2].Id);
                        await ctx.RespondAsync($"Log channels have been assigned:\n- {mentioned[0].Mention} (Command Logs)\n- {mentioned[1].Mention} (Message Logs)\n- {mentioned[2].Mention} (Action Logs)");
                    }
                    else
                    {
                        await ctx.RespondAsync("Timed out waiting for channel mentions.");
                    }
                }
            }
            else
            {
                await ctx.RespondAsync("Timed out waiting for response.");
            }
        }

        // Utility for other modules to log moderation actions
        public static async Task LogModerationAction(DiscordGuild guild, DiscordClient client, string action, DiscordUser target, DiscordUser moderator, string reason, TimeSpan? duration = null)
        {
            if (!LogChannels.TryGetValue(guild.Id, out var channels))
                return;

            var actionLogChannel = await client.GetChannelAsync(channels.actionLog);

            var embed = new DiscordEmbedBuilder
            {
                Title = $"Moderation Action: {action}",
                Description = $"**User:** {target.Mention} ({target.Id})\n" +
                              $"**Moderator:** {moderator.Mention} ({moderator.Id})\n" +
                              (duration.HasValue ? $"**Duration:** {duration.Value:g}\n" : "") +
                              $"**Reason:** {reason}",
                Color = DiscordColor.Orange,
                Timestamp = DateTimeOffset.UtcNow
            };

            await actionLogChannel.SendMessageAsync(embed: embed);
        }
    }
}