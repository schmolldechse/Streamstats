using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamstats.src.Service.Objects.Types
{
    /**
     * Type [cheer] , cheers via twitch
     */
    public class Cheer
    {

        /**
         * Amount of bits in cheer
         */
        public int amount { get; set; }

        /**
         * User provided message
         */
        public string? message { get; set; }

        public Activity activity { get; set; }

        public User user { get; set; }

        public Cheer(int amount, string? message, Activity activity, User user)
        {
            this.amount = amount;
            this.message = message;
            this.activity = activity;
            this.user = user;
        }
    }
}
