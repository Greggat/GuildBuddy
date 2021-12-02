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
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder().WithTitle($"{eventName}(0)").WithFooter("Click Attend to join the event"))
                .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "attend_event", "Attend"))
                );
        }
    }
}
