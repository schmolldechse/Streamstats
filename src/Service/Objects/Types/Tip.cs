using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamstats.src.Service.Objects.Types
{
    /**
     * Type [tip] , donation via streamelements
     */
    public class Tip
    {
        /**
         * StreamElements tip id
         */
        public string id { get; set; }

        /*
        * Amount of the donation
        */
        public decimal amount { get; set; }

        /**
         * Currency symbol spent with
         */
        public string currency { get; set; }

        /**
         * User provided message
         */
        public string? message { get; set; }

        public Activity activity { get; set; }

        public User user { get; set; }

        public Tip(string id, decimal amount, string currency, string message, Activity activity, User user)
        {
            this.id = id;
            this.amount = amount;
            this.currency = currency;
            this.message = message;

            this.activity = activity;
            this.user = user;
        }
    }
}
