using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using DSharpPlus.CommandsNext.Exceptions;
using GuildBuddy.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GuildBuddy.Services
{
    public class DiscordBotService : IHostedService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordClient _discord;
        private readonly IConfigurationRoot _config;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public DiscordBotService(
            IServiceProvider provider,
            DiscordClient discord,
            IConfigurationRoot config)
        {
            _provider = provider;
            _config = config;
            _discord = discord;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _provider.GetService(typeof(AuctionService));
            _provider.GetService(typeof(AttendenceEventService)); //Initialize Attendance Event Service

            _discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30),
                AckPaginationButtons = true
            });


            var slash = _discord.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = _provider
            });

            slash.RegisterCommands<SlashModule>(688586051563946008);
            slash.RegisterCommands<AuctionSlashModule>(688586051563946008);

            slash.RegisterCommands<SlashModule>(920138534826954822);
            slash.RegisterCommands<AuctionSlashModule>(920138534826954822);

            slash.SlashCommandErrored += OnSlashCommandErrored;

            await _discord.ConnectAsync();
        }

        private async Task OnSlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
        {
            if(e.Exception is SlashExecutionChecksFailedException)
            {
                var eb = new DiscordEmbedBuilder()
                    .WithTitle("Access Denied")
                    .WithDescription("You do not have permission to access this command. Please contact an admin if you think this is a mistake.");

                //Idk if commandsnext will have an interaction context?
                await e.Context.CreateResponseAsync(eb, true);
                e.Handled = true;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
