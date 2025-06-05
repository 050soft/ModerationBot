using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Moderation_Bot.commands;
using Moderation_Bot.config;

namespace Moderation_Bot
{
    public static class Program
    {
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static SlashCommandsExtension SlashCommands { get; private set; }

        static async Task Main(string[] args)
        {
            // Load the bot configuration
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            // Configure the Discord client
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);

            // Register Interactivity
            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            // Set up the CommandsNext module
            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { jsonReader.prefix },
                EnableDms = true,
                EnableMentionPrefix = true,
                EnableDefaultHelp = false
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.RegisterCommands<KickCommands>();
            Commands.RegisterCommands<BanCommands>();
            Commands.RegisterCommands<SnipeCommands>();
            Commands.RegisterCommands<LogsCommands>();
            Commands.RegisterCommands<TimeoutCommands>();

            // Set the bot's activity
            Client.Ready += Client_Ready;

            // Connect the bot
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            // Set the bot's activity to "Playing... Quantum flux"
            var activity = new DiscordActivity("MODERATING!!", ActivityType.Playing);
            sender.UpdateStatusAsync(activity);

            return Task.CompletedTask;
        }
    }
}