using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DSharpPlus;
using GuildBuddy.Services;

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
            services.AddSingleton(new DiscordClient(new DiscordConfiguration
            {
                Token = Configuration.GetValue<string>("tokens:discord"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            }))
            .AddSingleton(Configuration)            // Add the configuration to the collection
            .AddSingleton<AttendenceEventService>()

            .AddHostedService<DiscordBotService>();

            //Add Hangfire
            /*.AddHangfire(opt =>
             opt.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseColouredConsoleLogProvider()
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                $"User ID={Configuration["db_id"]};" +
                $"Password={Configuration["db_pw"]};" +
                $"Host={Configuration["db_host"]};" +
                $"Port={Configuration["db_port"]};" +
                $"Database={Configuration["db_name"]};" +
                "Pooling=true;")
            )
            .AddHangfireServer();*/
        }
    }
}