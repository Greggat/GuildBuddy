using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuildBuddy.Models.AuctionModels
{
    public class AuctionChannel
    {
        [Key]
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}
