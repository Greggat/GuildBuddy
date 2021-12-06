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
        private Dictionary<ulong, string> _cachedAttendeeMentions;

        public AttendenceEventService(DiscordClient client)
        {
            _client = client;
            _events = new Dictionary<ulong, HashSet<ulong>>();
            _cachedAttendeeMentions = new Dictionary<ulong, string>();

            _client.ComponentInteractionCreated += OnComponentInteractionCreated;
        }

        private async Task<List<string>> GetAttendeesNames(HashSet<ulong> attendeeIds)
        {
            List<string> result = new List<string>();
            foreach(var id in attendeeIds)
            {
                if (_cachedAttendeeMentions.TryGetValue(id, out var mention))
                {
                    result.Add(mention);
                }
                else
                {
                    //Shouldn't ever happen. TODO: log this anomoly?
                    var user = await _client.GetUserAsync(id);
                    _cachedAttendeeMentions.TryAdd(user.Id, user.Mention);
                    result.Add(user.Mention);
                }
                
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
                    _cachedAttendeeMentions.TryAdd(e.User.Id, e.User.Mention);

                    var attendees = await GetAttendeesNames(attendingUsers);
                    var title = e.Message.Embeds[0].Title;
                    title = title.Substring(0, title.Length - 2 - (attendees.Count / 10 + 1));

                    var eb = new DiscordEmbedBuilder()
                    .WithTitle($"{title}({attendees.Count})")
                    .WithFooter(e.Message.Embeds[0].Footer.Text)
                    .WithDescription(string.Join(", ", attendees));

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "attend_event", "Attend"))
                            .AddEmbed(eb.Build())
                        );

                    return;
                }
                else
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AsEphemeral(true).WithContent("You are already attending this event!"));
                }
            }
        }
    }
}
