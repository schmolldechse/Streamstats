using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamstats.src.Service.Objects
{
    public class Activity
    {

        /**
         * Time when the activity was created
         */
        public DateTime createdAt { get; set; }

        /**
         * Providers name of the activity
         */
        public string provider { get; set; }

        /**
         * Channel ID of the activity
         */
        public string channel { get; set; }

        /**
         * Type of the activity
         * Possible values [follow, tip, host, subscriber, cheer, redemption, sponsor, superchat]
         * Needed [tip, subscriber, cheer, superchat]
         */
        public string type { get; set; }

        /**
         * Id of the activity
         * Named as [_tip, activityId]
         */
        public string id { get; set; }

        public Activity(DateTime createdAt, string provider, string channel, string type, string id) 
        {
            this.createdAt = createdAt;
            this.provider = provider;
            this.channel = channel;
            this.type = type;
            this.id = id;
        }
    }
}
