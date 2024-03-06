using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketIOClient;
using SocketIO.Core;
using SocketIOClient.Transport;

namespace Streamstats.src.Service.Streamelements
{
    public class StreamelementsService
    {

        public readonly string STREAMELEMENTS_TIPS_API = "https://api.streamelements.com/kappa/v2/tips/channelId";

        public Boolean CONNECTED, FETCHED_DONATIONS;

        public List<Donation> donations;

        public string channelId = "";

        public SocketIOClient.SocketIO client;

        public StreamelementsService()
        {
            donations = new List<Donation>();

            CONNECTED = false;
            FETCHED_DONATIONS = false;
        }

        public async Task ConnectSocket()
        {
            client = new SocketIOClient.SocketIO("https://realtime.streamelements.com", new SocketIOOptions
            {
                EIO = EngineIO.V4,
                Transport = TransportProtocol.WebSocket
            });

            await client.ConnectAsync();

            client.On("authenticated", (data) =>
            {
                dynamic json = JsonConvert.DeserializeObject(data.ToString());
                Console.WriteLine($"Connected with streamelements | channelid = {json[0].channelId}");
                channelId = json[0].channelId;

                CONNECTED = true;
            });
            
            client.On("unauthorized", (data) =>
            {
                Console.WriteLine($"Failed to connect - Unauthorized {data}");

                CONNECTED = false;
            });

            client.On("unauthenticated", (data) =>
            {
                Console.WriteLine($"Failed to connect - Unauthenticated {data}");

                CONNECTED = false;
            });

            client.On("disconnect", (data) =>
            {
                Console.WriteLine($"Disconnected from websocket {data}");

                CONNECTED = false;
            });

            await client.EmitAsync("authenticate", new { method = "jwt", token = App.config.jwtToken });
        }

        public void fetchLatestTips()
        {
            Task.Run(async () =>
            {
                var response = await App.httpClient.GetAsync(STREAMELEMENTS_TIPS_API.Replace("channelId", channelId) + "?limit=100");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    dynamic jsonObject = JsonConvert.DeserializeObject<JObject>(responseContent);

                    foreach (JObject donation in jsonObject["docs"])
                    {
                        donations.Add(fetchDonation(donation));
                    }

                    Console.WriteLine($"Fetched {donations.Count} donations");
                    FETCHED_DONATIONS = true;
                }
                else
                {
                    Console.WriteLine($"Failed to fetch data: {response.StatusCode}");
                }
            });
        }

        public Donation fetchDonation(JObject donation)
        {
            string provider = donation["provider"].ToString();
            string channel = donation["channel"].ToString();
            DateTime createdAt = DateTime.Parse(donation["createdAt"].ToString());

            //TODO: check type [tip , follower]

            decimal amount = 0;
            string currency = null,
                username = null,
                email = null,
                transactionId = null,
                message = null;
            if (donation.ContainsKey("donation"))
            {
                amount = donation["donation"]["amount"].ToObject<decimal>();
                currency = donation["donation"]["currency"].ToString();
                username = donation["donation"]["user"]["username"].ToString();
                email = donation["donation"]["user"]["email"].ToString();
                transactionId = donation["transactionId"].ToString();
                message = donation["donation"]["message"].ToString();
            } else if (donation.ContainsKey("data")) { 
                amount = donation["data"]["amount"].ToObject<decimal>();
                currency = donation["data"]["currency"].ToString();
                username = donation["data"]["username"].ToString();
                //email = null;
                //transactionId = null;
                message = donation["data"]["message"].ToString();
            }

            string _id = donation["_id"].ToString();

            return new Donation(provider,
                channel,
                createdAt,
                new Data(amount,
                    currency,
                    username,
                    email,
                    transactionId,
                    message),
                _id
            );
        }
    }
}
