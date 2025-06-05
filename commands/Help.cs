using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

// Place this in a shared location, e.g., in the same file or a new file
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CategoryAttribute : Attribute
{
    public string Name { get; }
    public CategoryAttribute(string name) => Name = name;
}


namespace Moderation_Bot.commands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CategoryAttribute : Attribute
    {
        public string Name { get; }
        public CategoryAttribute(string name) => Name = name;
    }

    public class HelpCommands : BaseCommandModule
    {
        private const int CommandsPerPage = 5;

        [Command("help")]
        [Description("Shows a list of all commands.")]
        public async Task Help(CommandContext ctx)
        {
            await SendPaginatedHelp(ctx);
        }

        [Command("assistance")]
        [Description("Shows a list of all commands.")]
        public async Task Assistance(CommandContext ctx)
        {
            await SendPaginatedHelp(ctx);
        }

        private async Task SendPaginatedHelp(CommandContext ctx)
        {
            // Group commands by category
            var allCommands = ctx.CommandsNext.RegisteredCommands.Values
                .Distinct()
                .OrderBy(c => c.Name)
                .ToList();

            var categorized = new Dictionary<string, List<Command>>();

            foreach (var cmd in allCommands)
            {
                var catAttr = cmd.CustomAttributes?.FirstOrDefault(a => a.GetType().Name == "CategoryAttribute");
                string category = "Other";
                if (catAttr != null)
                {
                    var prop = catAttr.GetType().GetProperty("Name");
                    if (prop != null)
                    {
                        var value = prop.GetValue(catAttr) as string;
                        if (!string.IsNullOrWhiteSpace(value))
                            category = value;
                    }
                }
                if (!categorized.ContainsKey(category))
                    categorized[category] = new List<Command>();
                categorized[category].Add(cmd);
            }

            var categoryList = categorized.Keys.OrderBy(k => k).ToList();
            int currentCategoryIndex = 0;
            int currentPage = 1;

            Func<string, int, DiscordEmbedBuilder> getPageEmbed = (category, page) =>
            {
                var commands = categorized[category];
                int totalPages = (int)Math.Ceiling(commands.Count / (double)CommandsPerPage);

                var embed = new DiscordEmbedBuilder
                {
                    Title = $"📖 {category} Commands",
                    Color = DiscordColor.Blurple,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Category {currentCategoryIndex + 1}/{categoryList.Count} • Page {page}/{totalPages} • {commands.Count} commands"
                    }
                };

                int start = (page - 1) * CommandsPerPage;
                int end = Math.Min(start + CommandsPerPage, commands.Count);

                for (int i = start; i < end; i++)
                {
                    var cmd = commands[i];
                    var desc = string.IsNullOrWhiteSpace(cmd.Description) ? "No description." : cmd.Description;
                    string example = GetCommandExample(cmd);
                    var fieldValue = $"{desc}\n\n**Example:** `{example}`\n\n───────────────";
                    embed.AddField($".{cmd.Name}", fieldValue, false);
                }

                return embed;
            };

            var interactivity = ctx.Client.GetInteractivity();

            DiscordMessage sentMessage = null;

            while (true)
            {
                var commandsInCategory = categorized[categoryList[currentCategoryIndex]];
                int totalPages = (int)Math.Ceiling(commandsInCategory.Count / (double)CommandsPerPage);

                var prevCatButton = new DiscordButtonComponent(ButtonStyle.Secondary, "cat_prev", "⬆️ Prev Category", currentCategoryIndex == 0);
                var nextCatButton = new DiscordButtonComponent(ButtonStyle.Secondary, "cat_next", "Next Category ⬇️", currentCategoryIndex == categoryList.Count - 1);
                var prevButton = new DiscordButtonComponent(ButtonStyle.Primary, "help_prev", "⬅️ Previous", currentPage == 1);
                var nextButton = new DiscordButtonComponent(ButtonStyle.Primary, "help_next", "Next ➡️", currentPage == totalPages);

                var messageBuilder = new DiscordMessageBuilder()
                    .WithEmbed(getPageEmbed(categoryList[currentCategoryIndex], currentPage))
                    .AddComponents(prevCatButton, nextCatButton, prevButton, nextButton);

                if (sentMessage == null)
                    sentMessage = await ctx.Channel.SendMessageAsync(messageBuilder);
                else
                    await sentMessage.ModifyAsync(messageBuilder);

                var buttonResult = await interactivity.WaitForButtonAsync(sentMessage, ctx.User, TimeSpan.FromSeconds(60));
                if (buttonResult.TimedOut)
                {
                    // Disable all buttons after timeout
                    var disabledPrevCat = new DiscordButtonComponent(ButtonStyle.Secondary, "cat_prev", "⬆️ Prev Category", true);
                    var disabledNextCat = new DiscordButtonComponent(ButtonStyle.Secondary, "cat_next", "Next Category ⬇️", true);
                    var disabledPrev = new DiscordButtonComponent(ButtonStyle.Primary, "help_prev", "⬅️ Previous", true);
                    var disabledNext = new DiscordButtonComponent(ButtonStyle.Primary, "help_next", "Next ➡️", true);
                    var timeoutBuilder = new DiscordMessageBuilder()
                        .WithEmbed(getPageEmbed(categoryList[currentCategoryIndex], currentPage))
                        .AddComponents(disabledPrevCat, disabledNextCat, disabledPrev, disabledNext);
                    await sentMessage.ModifyAsync(timeoutBuilder);
                    break;
                }

                await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                if (buttonResult.Result.Id == "cat_prev" && currentCategoryIndex > 0)
                {
                    currentCategoryIndex--;
                    currentPage = 1;
                }
                else if (buttonResult.Result.Id == "cat_next" && currentCategoryIndex < categoryList.Count - 1)
                {
                    currentCategoryIndex++;
                    currentPage = 1;
                }
                else if (buttonResult.Result.Id == "help_prev" && currentPage > 1)
                {
                    currentPage--;
                }
                else if (buttonResult.Result.Id == "help_next" && currentPage < totalPages)
                {
                    currentPage++;
                }
            }
        }

        // Helper to get a usage example for a command
        private string GetCommandExample(Command c)
        {
            var usageAttr = c.CustomAttributes?.FirstOrDefault(a => a.GetType().Name == "UsageAttribute");
            if (usageAttr != null)
            {
                var prop = usageAttr.GetType().GetProperty("Example");
                if (prop != null)
                {
                    var value = prop.GetValue(usageAttr) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            var sb = new StringBuilder();
            sb.Append($".{c.Name}");
            if (c.Overloads != null && c.Overloads.Count > 0)
            {
                var args = c.Overloads[0].Arguments;
                foreach (var arg in args)
                {
                    sb.Append($" [{arg.Name}]");
                }
            }
            return sb.ToString();
        }
    }
}