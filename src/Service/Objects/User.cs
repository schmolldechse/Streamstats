using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamstats.src.Service.Objects
{
    public class User
    {

        /**
         * Username wo created the activity
         */
        public string username { get; set; }

        public User(string username)
        {
            this.username = username;
        }
    }
}
