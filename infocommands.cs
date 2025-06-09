using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Moderation_Bot.commands
{
    public class InfoCommands : BaseCommandModule
    {
        [Command("si")]
        [Description("Shows information about the server.")]
        public async Task ServerInfo(CommandContext ctx)
        {
            await SendServerInfo(ctx);
        }

        [Command("serverinfo")]
        [Description("Shows information about the server.")]
        public async Task ServerInfoAlias(CommandContext ctx)
        {
            await SendServerInfo(ctx);
        }

        private async Task SendServerInfo(CommandContext ctx)
        {
            var guild = ctx.Guild;
            var owner = guild.Owner;
            var roles = guild.Roles.Values.Where(r => !r.IsManaged && r.Id != guild.EveryoneRole.Id).OrderByDescending(r => r.Position).ToList();
            var boosts = guild.PremiumSubscriptionCount;
            var boostTier = guild.PremiumTier;
            var created = guild.CreationTimestamp.UtcDateTime;
            var iconUrl = guild.IconUrl;

            var embed = new DiscordEmbedBuilder
            {
                Title = $"Server Info: {guild.Name}",
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = iconUrl },
                Color = DiscordColor.Blurple,
                Timestamp = DateTimeOffset.UtcNow
            };

            embed.AddField("Owner", $"{owner.Mention} ({owner.Username}#{owner.Discriminator})", true);
            embed.AddField("Created", $"{created:yyyy-MM-dd HH:mm} UTC\n({(DateTime.UtcNow - created).Days} days ago)", true);
            embed.AddField("Members", guild.MemberCount.ToString(), true);
            embed.AddField("Roles", roles.Count.ToString(), true);
            embed.AddField("Boosts", $"{boosts} (Tier {((int)boostTier)})", true);

            // Show up to 10 roles, rest as count
            if (roles.Count > 0)
            {
                var roleList = string.Join(", ", roles.Take(10).Select(r => r.Mention));
                if (roles.Count > 10)
                    roleList += $" and {roles.Count - 10} more...";
                embed.AddField("Role List", roleList, false);
            }

            // Removed: embed.AddField("Region", guild.region ?? "Auto", true);

            // Optionally, add other info:
            embed.AddField("Verification Level", guild.VerificationLevel.ToString(), true);
            if (!string.IsNullOrWhiteSpace(guild.Description))
                embed.AddField("Description", guild.Description, false);

            embed.AddField("ID", guild.Id.ToString(), true);

            await ctx.Channel.SendMessageAsync(embed: embed);
        }

        [Command("ui")]
        [Description("Shows information about a user.")]
        public async Task UserInfo(CommandContext ctx, DiscordMember member = null)
        {
            await SendUserInfo(ctx, member);
        }

        [Command("userinfo")]
        [Description("Shows information about a user.")]
        public async Task UserInfoAlias(CommandContext ctx, DiscordMember member = null)
        {
            await SendUserInfo(ctx, member);
        }

        private async Task SendUserInfo(CommandContext ctx, DiscordMember member = null)
        {
            member = member ?? ctx.Member;
            var user = member;
            var joined = member.JoinedAt.UtcDateTime;
            var created = member.CreationTimestamp.UtcDateTime;
            var isBooster = member.PremiumSince.HasValue;
            var isNitro = user.AvatarUrl != user.DefaultAvatarUrl; // Not 100% accurate, but best available in DSharpPlus
            var roles = member.Roles.Where(r => r.Id != ctx.Guild.EveryoneRole.Id).OrderByDescending(r => r.Position).ToList();

            var embed = new DiscordEmbedBuilder
            {
                Title = $"User Info: {user.DisplayName}",
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = user.AvatarUrl },
                Color = DiscordColor.Blurple,
                Timestamp = DateTimeOffset.UtcNow
            };

            embed.AddField("Username", $"{user.Username}#{user.Discriminator}", true);
            embed.AddField("ID", user.Id.ToString(), true);
            embed.AddField("Account Created", $"{created:yyyy-MM-dd HH:mm} UTC\n({(DateTime.UtcNow - created).Days} days ago)", true);
            embed.AddField("Joined Server", $"{joined:yyyy-MM-dd HH:mm} UTC\n({(DateTime.UtcNow - joined).Days} days ago)", true);
            embed.AddField("Is Boosting", isBooster ? "Yes" : "No", true);
            embed.AddField("Has Nitro", isNitro ? "Likely" : "No", true);

            if (roles.Count > 0)
            {
                var roleList = string.Join(", ", roles.Take(10).Select(r => r.Mention));
                if (roles.Count > 10)
                    roleList += $" and {roles.Count - 10} more...";
                embed.AddField("Roles", roleList, false);
            }

            await ctx.Channel.SendMessageAsync(embed: embed);
        }
    }
}