using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuildBuddy.Models.AuctionModels
{
    public class AuctionRole
    {
        public ulong GuildId { get; set; }
        [Key]
        public ulong RoleId { get; set; }
        public AuctionPermissions Permissions { get; set; }
    }
}
