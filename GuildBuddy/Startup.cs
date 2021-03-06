using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DSharpPlus;
using GuildBuddy.Services;
using System.Reflection;
using Hangfire.Storage.SQLite;

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

            .AddDbContext<GuildBuddyContext>()

            .AddSingleton<AttendenceEventService>()
            .AddSingleton<AuctionService>()

            .AddHostedService<DiscordBotService>()

            //Add Hangfire'
            .AddHangfire(opt =>
             opt//.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                //.UseColouredConsoleLogProvider()
                //.UseSimpleAssemblyNameTypeSerializer()
                //.UseRecommendedSerializerSettings()
                //.UseSQLiteStorage("Data Source=" + Path.Join(dir, "hangfire.db") + ";")
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage(Path.Join(dir, "hangfire.db"))
            )
            .AddHangfireServer();

            
        }
    }
}