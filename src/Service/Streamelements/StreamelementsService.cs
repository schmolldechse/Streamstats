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

        private readonly string STREAMELEMENTS_TIPS_API = "https://api.streamelements.com/kappa/v2/tips/channelId";

        public Boolean CONNECTED;

        public List<Donation> donations;

        private string channelId = "";

        public StreamelementsService()
        {
            donations = new List<Donation>();
            donations.Sort((donation1, donation2) => donation1.createdAt.CompareTo(donation2.createdAt));

            CONNECTED = false;
        }

        public async Task ConnectSocket()
        {
            SocketIOClient.SocketIO client = new SocketIOClient.SocketIO("https://realtime.streamelements.com", new SocketIOOptions
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

            client.On("event", (data) =>
            {
                Console.WriteLine($"Received a new donation {data}");

                handleIncomingDonation(data.ToString());
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

        public void handleIncomingDonation(string data)
        {
            JArray jArray = JArray.Parse(data);
            JObject donation = (JObject)jArray.First;

            string provider = donation["provider"].ToString();
            string channel = donation["channel"].ToString();
            DateTime createdAt = DateTime.Parse(donation["createdAt"].ToString());

            int amount = donation["data"]["amount"].ToObject<int>();
            string currency = donation["data"]["currency"].ToString();
            string username = donation["data"]["username"].ToString();
            string transactionId = donation["data"]["tipId"].ToString();
            string message = donation["data"]["message"].ToString();

            string _id = donation["_id"].ToString();

            Donation fetched = fetchDonation(donation);

            donations.Insert(0, fetched);
            //Panel.donation_Panel.AddLatestTip(donation);
        }

        public void fetchLatestTips()
        {
            Task.Run(async () =>
            {
                var response = await App.httpClient.GetAsync(STREAMELEMENTS_TIPS_API.Replace("channelId", channelId) + "?limit=50");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    dynamic jsonObject = JsonConvert.DeserializeObject<JObject>(responseContent);

                    foreach (JObject donation in jsonObject["docs"])
                    {
                        donations.Add(fetchDonation(donation));
                    }

                    Console.WriteLine($"Fetched {donations.Count} donations");
                }
                else
                {
                    Console.WriteLine($"Failed to fetch data: {response.StatusCode}");
                }
            });
        }

        private Donation fetchDonation(JObject donation)
        {
            string provider = donation["provider"].ToString();
            string channel = donation["channel"].ToString();
            DateTime createdAt = DateTime.Parse(donation["createdAt"].ToString());

            int amount = donation["donation"]["amount"].ToObject<int>();
            string currency = donation["donation"]["currency"].ToString();
            string username = donation["donation"]["user"]["username"].ToString();
            string email = donation["donation"]["user"]["email"].ToString();
            string transactionId = donation["transactionId"].ToString();
            string message = donation["donation"]["message"].ToString();

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
