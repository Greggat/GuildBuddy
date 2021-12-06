using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.SQLite;
using Hangfire.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DSharpPlus;
using GuildBuddy.Services;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using System.Reflection;

namespace GuildBuddy
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup(string[] args)
        {
        }

        public static async Task RunAsync(string[] args)
        {
            var startup = new Startup(args);
            await startup.StartAsync(args);
        }

        public async Task StartAsync(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args);
            host.ConfigureHostConfiguration(opt =>
            {
                opt.SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json");
                Configuration = opt.Build();
            });
            host.ConfigureServices(services =>
            {
                ConfigureServices(services);
            });

            await host.Build().RunAsync();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            services.AddSingleton(new DiscordClient(new DiscordConfiguration
            {
                Token = Configuration.GetValue<string>("tokens:discord"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            }))
            .AddSingleton(Configuration)            // Add the configuration to the collection
            .AddSingleton<AttendenceEventService>()
            .AddSingleton<AuctionService>()

            .AddHostedService<DiscordBotService>()

            //Add Hangfire'
            .AddHangfire(opt =>
             opt.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseColouredConsoleLogProvider()
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage("Data Source=" + Path.Join(dir, "hangfire.db") + ";")
            )
            .AddHangfireServer();

            
        }
    }
}