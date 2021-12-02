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
        private Dictionary<ulong, HashSet<ulong>> _events;

        public AttendenceEventService(DiscordClient client)
        {
            _client = client;
            _events = new Dictionary<ulong, HashSet<ulong>>();

            _client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }

        private async Task<List<string>> GetAttendeesNames(HashSet<ulong> attendeeIds)
        {
            List<string> result = new List<string>();
            foreach(var id in attendeeIds)
            {
                var user = await _client.GetUserAsync(id);
                result.Add(user.Username);
            }
            return result;
        }

        private async Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            if (e.Id == "attend_event")
            {
                if (!_events.TryGetValue(e.Message.Id, out var attendingUsers))
                {
                    attendingUsers = new HashSet<ulong>();
                    _events.Add(e.Message.Id, attendingUsers);
                }
                if(attendingUsers.Add(e.User.Id))
                {
                    var attendees = await GetAttendeesNames(attendingUsers);
                    var title = e.Message.Embeds[0].Title;
                    title = title.Substring(0, title.Length - 2 - (attendees.Count / 10 + 1));
                    var eb = new DiscordEmbedBuilder()
                    .WithTitle($"{title}({attendees.Count})")
                    .WithFooter(e.Message.Embeds[0].Footer.Text)
                    .WithDescription(String.Join(",", attendees));
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "attend_event", "Attend"))
                            .WithContent(e.User.Username)
                            .AddEmbed(eb.Build())
                        );

                    return;
                }
            }

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                  new DiscordInteractionResponseBuilder().AsEphemeral(true).WithContent("You are already attending this event!"));
        }
    }
}
