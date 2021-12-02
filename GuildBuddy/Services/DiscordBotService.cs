using DSharpPlus;
using DSharpPlus.SlashCommands;
using GuildBuddy.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
            _provider.GetService(typeof(AttendenceEventService)); //Initialize Attendance Event Service

            var slash = _discord.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = _provider
            });

            slash.RegisterCommands<SlashModule>(688586051563946008);

            await _discord.ConnectAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
