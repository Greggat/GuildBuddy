using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GuildBuddy.Services;

namespace GuildBuddy.Modules
{
    public class SlashModule : ApplicationCommandModule
    {
        private AttendenceEventService _attendenceEventService;

        public SlashModule(AttendenceEventService attend)
        {
            _attendenceEventService = attend;
        }

        [SlashCommand("createattendevent", "Creates an attendance event")]
        public async Task CreateAttendanceEvent(InteractionContext ctx, [Option("eventname", "The name of the attendance event you want to create")] string eventName)
        {
            Console.WriteLine("wtf man");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "attend_event", "Attend"))
                .WithContent("test")
                );
        }

        [SlashCommand("test", "test")]
        public async Task Test(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .WithContent("Method: test")
                );
        }
    }
}
