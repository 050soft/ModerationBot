using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Moderation_Bot.commands
{
    public class BanCommands : BaseCommandModule
    {
        private bool HasBanPermission(CommandContext ctx) =>
            ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.BanMembers);

        private bool CanBeBanned(CommandContext ctx, DiscordMember target) =>
            ctx.Member.Hierarchy > target.Hierarchy && !target.IsBot;

        private async Task RespondEmbed(CommandContext ctx, string title, string description, DiscordColor? color = null)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = title,
                Description = description,
                Color = color ?? DiscordColor.Red
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("ban")]
        [Description("Bans a user for a specified duration (days or 'perm') and reason. Usage: .ban @user 7, Spamming")]
        public async Task Ban(CommandContext ctx, DiscordMember target, [RemainingText] string args)
        {
            await HandleBan(ctx, target, args, "ban");
        }

        [Command("banish")]
        [Description("Bans a user for a specified duration (days or 'perm') and reason. Usage: .banish @user perm, Rule violation")]
        public async Task Banish(CommandContext ctx, DiscordMember target, [RemainingText] string args)
        {
            await HandleBan(ctx, target, args, "banish");
        }

        private async Task HandleBan(CommandContext ctx, DiscordMember target, string args, string commandName)
        {
            if (!HasBanPermission(ctx))
            {
                await RespondEmbed(ctx, "Insufficient Permissions", "You do not have permission to ban members.");
                return;
            }

            if (!CanBeBanned(ctx, target))
            {
                await RespondEmbed(ctx, "Cannot Ban", "You cannot ban this user.");
                return;
            }

            if (string.IsNullOrWhiteSpace(args) || !args.Contains(","))
            {
                await RespondEmbed(ctx, "Invalid Usage", $"Usage: .{commandName} @user <days|perm>, <reason>");
                return;
            }

            var split = args.Split(new[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var durationInput = split[0].Trim();
            var reason = split.Length > 1 ? split[1].Trim() : "No reason provided";

            int deleteDays = 0;
            if (durationInput.Equals("perm", StringComparison.OrdinalIgnoreCase))
            {
                deleteDays = 7; // Discord's max days for message deletion
            }
            else if (!int.TryParse(durationInput, out deleteDays) || deleteDays < 0 || deleteDays > 7)
            {
                await RespondEmbed(ctx, "Invalid Duration", "Duration must be a number of days (0-7) or 'perm' for permanent.");
                return;
            }

            await target.BanAsync(deleteDays, reason);

            var embed = new DiscordEmbedBuilder
            {
                Title = "User Banned",
                Description = $"{target.Mention} has been banned.\nDuration: {(durationInput.Equals("perm", StringComparison.OrdinalIgnoreCase) ? "Permanent" : $"{deleteDays} day(s) of message deletion")}\nReason: {reason}",
                Color = DiscordColor.Orange
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("unban")]
        [Description("Unbans a user by their user ID. Usage: .unban <userId> [reason]")]
        public async Task Unban(CommandContext ctx, ulong userId, [RemainingText] string reason = "No reason provided")
        {
            await HandleUnban(ctx, userId, reason, "unban");
        }

        [Command("unbanish")]
        [Description("Unbans a user by their user ID. Usage: .unbanish <userId> [reason]")]
        public async Task Unbanish(CommandContext ctx, ulong userId, [RemainingText] string reason = "No reason provided")
        {
            await HandleUnban(ctx, userId, reason, "unbanish");
        }

        private async Task HandleUnban(CommandContext ctx, ulong userId, string reason, string commandName)
        {
            if (!HasBanPermission(ctx))
            {
                await RespondEmbed(ctx, "Insufficient Permissions", "You do not have permission to unban members.");
                return;
            }

            var bans = await ctx.Guild.GetBansAsync();
            var ban = bans.FirstOrDefault(b => b.User.Id == userId);

            if (ban == null)
            {
                await RespondEmbed(ctx, "User Not Banned", $"No banned user with ID `{userId}` found.");
                return;
            }

            await ctx.Guild.UnbanMemberAsync(userId, reason);

            var embed = new DiscordEmbedBuilder
            {
                Title = "User Unbanned",
                Description = $"User with ID `{userId}` has been unbanned.\nReason: {reason}",
                Color = DiscordColor.Green
            };
            await ctx.RespondAsync(embed: embed);
        }
    }
}