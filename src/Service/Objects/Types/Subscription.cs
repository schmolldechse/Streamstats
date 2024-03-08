using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamstats.src.Service.Objects.Types
{

    /**
     * Type [subscriber] , subscription via twitch
     */
    public class Subscription
    {

        /**
         * Subscribed for ... months
         */
        public int amount { get; set; }

        /**
         * Subscriber tier
         * Possible values [1000, 2000, 3000, prime]
         * 1000 - Tier 1
         * 2000 - Tier 2
         * 3000 - Tier 3
         * prime - Twitch Prime
         */
        public string tier { get; set; }

        /**
         * User provided message
         */
        public string? message { get; set; }

        /**
         * True if the subscription is gifted to the user
         */
        public bool gifted { get; set; }


        public Activity activity { get; set; }

        public User user { get; set; }
        public User? sent { get; set; }

        public Subscription(int amount, string tier, string? message, bool gifted, Activity activity, User user, User? sent)
        {
            this.amount = amount;
            this.tier = tier;
            this.message = message;
            this.gifted = gifted;
            this.activity = activity;
            this.user = user;
            this.sent = sent;
        }
    }
}
