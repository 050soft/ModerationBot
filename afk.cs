using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Moderation_Bot.commands
{
    public class AfkCommands : BaseCommandModule
    {
        // Stores AFK status: userId -> (reason, setTime)
        public static ConcurrentDictionary<ulong, (string Reason, DateTime SetTime)> AfkStatuses
            = new ConcurrentDictionary<ulong, (string, DateTime)>();

        [Command("afk")]
        [Description("Set your AFK status with an optional reason.")]
        public async Task Afk(CommandContext ctx, [RemainingText] string reason = "AFK")
        {
            AfkStatuses[ctx.User.Id] = (reason, DateTime.UtcNow);
            await ctx.RespondAsync($"{ctx.User.Mention}, you are now marked as AFK: **{reason}**");
        }

        // Call this from your Program.cs in the MessageCreated event!
        public static async Task HandleAfkMentions(DiscordClient client, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.MentionedUsers == null || e.MentionedUsers.Count == 0)
                return;

            foreach (var user in e.MentionedUsers)
            {
                if (AfkStatuses.TryGetValue(user.Id, out var afk))
                {
                    var since = DateTime.UtcNow - afk.SetTime;
                    var sinceStr = since.TotalMinutes < 1 ? "just now" :
                        since.TotalHours < 1 ? $"{(int)since.TotalMinutes}m ago" :
                        since.TotalDays < 1 ? $"{(int)since.TotalHours}h ago" :
                        $"{(int)since.TotalDays}d ago";

                    await e.Message.RespondAsync(
                        $"{user.Mention} is currently AFK: **{afk.Reason}** (set {sinceStr})"
                    );
                }
            }

            // Optionally: Remove AFK if the author is marked AFK and sends a message
            if (AfkStatuses.ContainsKey(e.Author.Id))
            {
                AfkStatuses.TryRemove(e.Author.Id, out _);
                await e.Message.RespondAsync($"{e.Author.Mention}, your AFK status has been removed.");
            }
        }
    }
}
