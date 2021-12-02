using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace GuildBuddy.Services
{
    public class AttendenceEventService
    {
        private DiscordClient _client;
        private Dictionary<uint, List<DiscordMember>> _events;

        public AttendenceEventService(DiscordClient client)
        {
            _client = client;
            _events = new Dictionary<uint, List<DiscordMember>>();

            _client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }

        private async Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            if (e.Id == "attend_event")
            {
                //do attend stuff
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                    new DiscordInteractionResponseBuilder().AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "attend_event", "Attend")).WithContent(e.User.Username));
            }
        }
    }
}
