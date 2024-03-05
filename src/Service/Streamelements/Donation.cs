using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamstats.src.Service.Streamelements
{
    public class Donation
    {
        public string provider { get; set; }
        public string channel { get; set; }
        public DateTime createdAt { get; set; }
        public Data data { get; set; }
        public string _id { get; set; }

        public Donation(string provider, string channel, DateTime createdAt, Data data, string id)
        {
            this.provider = provider;
            this.channel = channel;
            this.createdAt = createdAt;
            this.data = data;
            _id = id;
        }

        public override string ToString()
        {
            return $"Donation: {provider}, Channel: {channel}, CreatedAt: {createdAt:yyyy-MM-dd HH:mm:ss}, Data: [{data}], Id: {_id}";
        }
    }

    public class Data
    {
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string activityId { get; set; }
        public string message { get; set; }

        public Data(decimal amount, string currency, string username, string email, string activityId, string message)
        {
            this.amount = amount;
            this.currency = currency;
            this.username = username;
            this.email = email;
            this.activityId = activityId;
            this.message = message;
        }

        public override string ToString()
        {
            return $"{amount}, Currency: {currency}, username: {username}, email: {email}, ActivityId: {activityId}, Message: {message}";
        }
    }
}