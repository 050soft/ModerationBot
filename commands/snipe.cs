using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Moderation_Bot.commands
{
    public class SnipeCommands : BaseCommandModule
    {
        // Store last 5 deleted messages per channel
        private static readonly ConcurrentDictionary<ulong, LinkedList<(string Author, string Content, DateTimeOffset Timestamp)>> DeletedMessages
            = new ConcurrentDictionary<ulong, LinkedList<(string, string, DateTimeOffset)>>();

        // Store last 5 edited messages per channel
        private static readonly ConcurrentDictionary<ulong, LinkedList<(string Author, string OldContent, string NewContent, DateTimeOffset Timestamp)>> EditedMessages
            = new ConcurrentDictionary<ulong, LinkedList<(string, string, string, DateTimeOffset)>>();

        // Call this once in your bot's setup (e.g., after client is ready)
        public static void RegisterEventHandlers(DiscordClient client)
        {
            client.MessageDeleted += async (s, e) =>
            {
                if (e.Message == null || string.IsNullOrWhiteSpace(e.Message.Content))
                    return;

                var list = DeletedMessages.GetOrAdd(e.Channel.Id, _ => new LinkedList<(string, string, DateTimeOffset)>());
                list.AddFirst((e.Message.Author?.Username ?? "Unknown", e.Message.Content, e.Message.Timestamp));
                while (list.Count > 5)
                    list.RemoveLast();
            };

            client.MessageUpdated += async (s, e) =>
            {
                if (e.Message == null || e.MessageBefore == null || string.IsNullOrWhiteSpace(e.MessageBefore.Content))
                    return;

                // Only store if content actually changed
                if (e.MessageBefore.Content == e.Message.Content)
                    return;

                var list = EditedMessages.GetOrAdd(e.Channel.Id, _ => new LinkedList<(string, string, string, DateTimeOffset)>());
                list.AddFirst((e.Message.Author?.Username ?? "Unknown", e.MessageBefore.Content, e.Message.Content, DateTimeOffset.UtcNow));
                while (list.Count > 5)
                    list.RemoveLast();
            };
        }

        [Command("snipe")]
        [Description("Shows the last 5 deleted messages in this channel with navigation buttons.")]
        public async Task Snipe(CommandContext ctx)
        {
            if (!DeletedMessages.TryGetValue(ctx.Channel.Id, out var list) || list.Count == 0)
            {
                await ctx.RespondAsync("There's nothing to snipe!");
                return;
            }

            var messages = list.ToList();
            int index = 0;

            var embed = BuildSnipeEmbed(messages, index);
            var left = new DiscordButtonComponent(ButtonStyle.Secondary, "snipe_left", "⬅️", disabled: true);
            var right = new DiscordButtonComponent(ButtonStyle.Secondary, "snipe_right", "➡️", disabled: messages.Count <= 1);

            var msg = await ctx.Channel.SendMessageAsync(
                new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .AddComponents(left, right)
            );

            async Task Handler(DiscordClient client, ComponentInteractionCreateEventArgs e)
            {
                if (e.Message.Id != msg.Id || e.User.Id != ctx.User.Id)
                    return;

                if (e.Id == "snipe_left" && index > 0)
                    index--;
                else if (e.Id == "snipe_right" && index < messages.Count - 1)
                    index++;

                var newEmbed = BuildSnipeEmbed(messages, index);
                var newLeft = new DiscordButtonComponent(ButtonStyle.Secondary, "snipe_left", "⬅️", disabled: index == 0);
                var newRight = new DiscordButtonComponent(ButtonStyle.Secondary, "snipe_right", "➡️", disabled: index == messages.Count - 1);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(newEmbed)
                        .AddComponents(newLeft, newRight));
            }

            ctx.Client.ComponentInteractionCreated += Handler;

            // Optionally, remove the handler after a timeout to avoid memory leaks
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(2));
                ctx.Client.ComponentInteractionCreated -= Handler;
            });
        }

        [Command("editsnipe")]
        [Description("Shows the last 5 edited messages in this channel with navigation buttons.")]
        public async Task EditSnipe(CommandContext ctx)
        {
            if (!EditedMessages.TryGetValue(ctx.Channel.Id, out var list) || list.Count == 0)
            {
                await ctx.RespondAsync("There's nothing to editsnipe!");
                return;
            }

            var messages = list.ToList();
            int index = 0;

            var embed = BuildEditSnipeEmbed(messages, index);
            var left = new DiscordButtonComponent(ButtonStyle.Secondary, "editsnipe_left", "⬅️", disabled: true);
            var right = new DiscordButtonComponent(ButtonStyle.Secondary, "editsnipe_right", "➡️", disabled: messages.Count <= 1);

            var msg = await ctx.Channel.SendMessageAsync(
                new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .AddComponents(left, right)
            );

            async Task Handler(DiscordClient client, ComponentInteractionCreateEventArgs e)
            {
                if (e.Message.Id != msg.Id || e.User.Id != ctx.User.Id)
                    return;

                if (e.Id == "editsnipe_left" && index > 0)
                    index--;
                else if (e.Id == "editsnipe_right" && index < messages.Count - 1)
                    index++;

                var newEmbed = BuildEditSnipeEmbed(messages, index);
                var newLeft = new DiscordButtonComponent(ButtonStyle.Secondary, "editsnipe_left", "⬅️", disabled: index == 0);
                var newRight = new DiscordButtonComponent(ButtonStyle.Secondary, "editsnipe_right", "➡️", disabled: index == messages.Count - 1);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(newEmbed)
                        .AddComponents(newLeft, newRight));
            }

            ctx.Client.ComponentInteractionCreated += Handler;

            // Optionally, remove the handler after a timeout to avoid memory leaks
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(2));
                ctx.Client.ComponentInteractionCreated -= Handler;
            });
        }

        private static DiscordEmbed BuildSnipeEmbed(List<(string Author, string Content, DateTimeOffset Timestamp)> messages, int index)
        {
            var (author, content, timestamp) = messages[index];
            return new DiscordEmbedBuilder
            {
                Title = $"Deleted Message #{index + 1}",
                Description = $"**Author:** {author}\n**Time:** {timestamp.LocalDateTime:t}\n\n{(string.IsNullOrWhiteSpace(content) ? "*[No content]*" : content)}",
                Color = DiscordColor.Orange
            };
        }

        private static DiscordEmbed BuildEditSnipeEmbed(List<(string Author, string OldContent, string NewContent, DateTimeOffset Timestamp)> messages, int index)
        {
            var (author, oldContent, newContent, timestamp) = messages[index];
            return new DiscordEmbedBuilder
            {
                Title = $"Edited Message #{index + 1}",
                Description = $"**Author:** {author}\n**Time:** {timestamp.LocalDateTime:t}\n\n" +
                              $"**Before:** {(string.IsNullOrWhiteSpace(oldContent) ? "*[No content]*" : oldContent)}\n" +
                              $"**After:** {(string.IsNullOrWhiteSpace(newContent) ? "*[No content]*" : newContent)}",
                Color = DiscordColor.Blurple
            };
        }
    }
}