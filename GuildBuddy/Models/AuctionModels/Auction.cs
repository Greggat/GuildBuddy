using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuildBuddy.Models.AuctionModels
{
    [Index(nameof(GuildId))]
    public class Auction
    {
        public ulong Id { get; set; }
        public ulong GuildId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public double CurrentBid { get; set; } = 0;
        public double MinIncrement { get; set; } = 0;
        public ulong BidderId { get; set; } = 0;
        public string BidderName { get; set; } = "none";
        public DateTime Expiration { get; set; } = DateTime.MinValue;
        public bool AutoExtend { get; set; } = false;
        public string JobId { get; set; } = string.Empty;
    }
}
