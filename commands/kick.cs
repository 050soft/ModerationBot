using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Moderation_Bot.commands
{
    public class KickCommands : BaseCommandModule
    {
        private bool HasKickPermission(CommandContext ctx) =>
            ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.KickMembers);

        private bool CanBeKicked(CommandContext ctx, DiscordMember target) =>
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

        [Command("kick")]
        [Description("Kicks a user by their user ID. Usage: .kick <userId> [reason]")]
        public async Task Kick(CommandContext ctx, ulong userId, [RemainingText] string reason = "No reason provided")
        {
            await HandleKick(ctx, userId, reason, "kick");
        }

        [Command("boot")]
        [Description("Kicks a user by their user ID. Usage: .boot <userId> [reason]")]
        public async Task Boot(CommandContext ctx, ulong userId, [RemainingText] string reason = "No reason provided")
        {
            await HandleKick(ctx, userId, reason, "boot");
        }

        private async Task HandleKick(CommandContext ctx, ulong userId, string reason, string commandName)
        {
            if (!HasKickPermission(ctx))
            {
                await RespondEmbed(ctx, "Insufficient Permissions", "You do not have permission to kick members.");
                return;
            }

            DiscordMember target = null;
            try
            {
                target = await ctx.Guild.GetMemberAsync(userId);
            }
            catch
            {
                await RespondEmbed(ctx, "User Not Found", $"No member with ID `{userId}` found in this server.");
                return;
            }

            if (!CanBeKicked(ctx, target))
            {
                await RespondEmbed(ctx, "Cannot Kick", "You cannot kick this user.");
                return;
            }

            await target.RemoveAsync(reason);

            var embed = new DiscordEmbedBuilder
            {
                Title = "User Kicked",
                Description = $"{target.Mention} has been kicked.\nReason: {reason}",
                Color = DiscordColor.Orange
            };
            await ctx.RespondAsync(embed: embed);
        }
    }
}