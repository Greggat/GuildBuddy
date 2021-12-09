using System.ComponentModel.DataAnnotations;

namespace GuildBuddy.Models.AuctionModels
{
    public class AuctionChannel
    {
        [Key]
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}
