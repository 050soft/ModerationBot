using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Moderation_Bot.commands
{
    public class TimeoutCommands : BaseCommandModule
    {
        // Helper method to check permissions and mute status
        private bool HasMutePermission(CommandContext ctx) =>
            ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.MuteMembers);

        private bool CanBeMuted(CommandContext ctx, DiscordMember target) =>
            ctx.Member.Hierarchy > target.Hierarchy && !target.IsBot;

        private bool IsAlreadyMuted(DiscordMember target) =>
            target.CommunicationDisabledUntil.HasValue && target.CommunicationDisabledUntil > DateTimeOffset.UtcNow;

        private async Task RespondEmbed(CommandContext ctx, string title, string description)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = title,
                Description = description,
                Color = DiscordColor.Red
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("mute")]
        [Description("Timeouts a user for a specified duration and reason.")]
        public async Task Mute(CommandContext ctx, DiscordMember target, TimeSpan duration, [RemainingText] string reason = "No reason provided")
        {
            await HandleTimeout(ctx, target, duration, reason, "mute");
        }

        [Command("timeout")]
        [Description("Timeouts a user for a specified duration and reason.")]
        public async Task Timeout(CommandContext ctx, DiscordMember target, TimeSpan duration, [RemainingText] string reason = "No reason provided")
        {
            await HandleTimeout(ctx, target, duration, reason, "timeout");
        }

        [Command("shut")]
        [Description("Timeouts a user for a specified duration and reason.")]
        public async Task Shut(CommandContext ctx, DiscordMember target, TimeSpan duration, [RemainingText] string reason = "No reason provided")
        {
            await HandleTimeout(ctx, target, duration, reason, "shut");
        }

        [Command("stfu")]
        [Description("Timeouts a user for a specified duration and reason.")]
        public async Task Stfu(CommandContext ctx, DiscordMember target, TimeSpan duration, [RemainingText] string reason = "No reason provided")
        {
            await HandleTimeout(ctx, target, duration, reason, "stfu");
        }

        private async Task HandleTimeout(CommandContext ctx, DiscordMember target, TimeSpan duration, string reason, string commandName)
        {
            if (IsAlreadyMuted(target))
            {
                await RespondEmbed(ctx, "Already Muted", $"{target.Mention} is already muted.");
                return;
            }

            if (!HasMutePermission(ctx))
            {
                await RespondEmbed(ctx, "Insufficient Permissions", "You do not have permission to mute members.");
                return;
            }

            if (!CanBeMuted(ctx, target))
            {
                await RespondEmbed(ctx, "Cannot Mute", "You cannot mute this user.");
                return;
            }

            // Convert TimeSpan to DateTimeOffset by adding it to the current UTC time
            var timeoutUntil = DateTimeOffset.UtcNow.Add(duration);
            await target.TimeoutAsync(timeoutUntil, reason);

            var embed = new DiscordEmbedBuilder
            {
                Title = "User Muted",
                Description = $"{target.Mention} has been muted for {duration:g}.\nReason: {reason}",
                Color = DiscordColor.Green
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("unmute")]
        [Description("Removes timeout from a user.")]
        public async Task Unmute(CommandContext ctx, DiscordMember target, [RemainingText] string reason = "No reason provided")
        {
            await HandleUnmute(ctx, target, reason, "unmute");
        }

        [Command("unshut")]
        [Description("Removes timeout from a user.")]
        public async Task Unshut(CommandContext ctx, DiscordMember target, [RemainingText] string reason = "No reason provided")
        {
            await HandleUnmute(ctx, target, reason, "unshut");
        }

        private async Task HandleUnmute(CommandContext ctx, DiscordMember target, string reason, string commandName)
        {
            if (!HasMutePermission(ctx))
            {
                await RespondEmbed(ctx, "Insufficient Permissions", "You do not have permission to unmute members.");
                return;
            }

            if (!IsAlreadyMuted(target))
            {
                await RespondEmbed(ctx, "Not Muted", $"{target.Mention} is not currently muted.");
                return;
            }

            await target.TimeoutAsync(null, reason);

            var embed = new DiscordEmbedBuilder
            {
                Title = "User Unmuted",
                Description = $"{target.Mention} has been unmuted.\nReason: {reason}",
                Color = DiscordColor.Green
            };
            await ctx.RespondAsync(embed: embed);
        }
    }
}