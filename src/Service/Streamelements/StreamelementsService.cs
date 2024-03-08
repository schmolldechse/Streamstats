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
using Streamstats.src.Service.Objects.Types;
using Streamstats.src.Service.Objects;

namespace Streamstats.src.Service.Streamelements
{
    public class StreamelementsService
    {

        public readonly string STREAMELEMENTS_ACTIVITIES_API = "https://api.streamelements.com/kappa/v2/activities/{0}";
        private readonly string REQUEST_TYPES = "[\"tip\",\"subscriber\",\"cheer\"]";

        public List<Tip> fetchedDonations;
        public List<Subscription> fetchedSubscriptions;
        public List<Cheer> fetchedCheers;

        public string channelId = "";

        public SocketIOClient.SocketIO client;

        public StreamelementsService()
        {
            fetchedDonations = new List<Tip>();
            fetchedSubscriptions = new List<Subscription>();
            fetchedCheers = new List<Cheer>();
        }

        public async Task ConnectSocket(Action<bool, string?> callback)
        {
            client = new SocketIOClient.SocketIO("https://realtime.streamelements.com", new SocketIOOptions
            {
                EIO = EngineIO.V4,
                Transport = TransportProtocol.WebSocket
            });

            await client.ConnectAsync();

            client.On("authenticated", (data) =>
            {
                dynamic? json = JsonConvert.DeserializeObject(data.ToString());
                Console.WriteLine($"Connected with streamelements | channelid = {json?[0].channelId}");
                channelId = json[0].channelId;

                callback?.Invoke(true, null);
            });
            
            client.On("unauthorized", (data) =>
            {
                Console.WriteLine($"Failed to connect - Unauthorized {data}");
                callback?.Invoke(false, data.ToString());
            });

            client.On("unauthenticated", (data) =>
            {
                Console.WriteLine($"Failed to connect - Unauthenticated {data}");
                callback?.Invoke(false, data.ToString());
            });

            client.On("disconnect", (data) =>
            {
                Console.WriteLine($"Disconnected from websocket {data}");
                callback?.Invoke(false, data.ToString());
            });

            await client.EmitAsync("authenticate", new { method = "jwt", token = App.config.jwtToken });
        }

        /**
         * Current date - goBack
         */
        public async Task fetchLatest(int goBack, Action<bool> done)
        {
            DateTime current = DateTime.Now;
            DateTime start = current.AddDays(-goBack);

            List<JObject> fetchedObjects = new List<JObject>();

            for (DateTime date = current; date >= start; date = date.AddDays(-1))
            {
                string before = date.AddDays(1).ToString("yyyy-MM-dd");
                string after = date.ToString("yyyy-MM-dd");

                var response = await App.httpClient.GetAsync(string.Format(STREAMELEMENTS_ACTIVITIES_API, this.channelId) + $"?after={after}&before={before}&limit=100&types={REQUEST_TYPES}&origin=feed");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    JArray jArray = JArray.Parse(responseContent);

                    foreach (JObject document in jArray)
                    {
                        if (document["_id"] == null || document["type"] == null) continue;
                        fetchedObjects.Add(document);
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to fetch data for {date.ToShortDateString()}");
                }

                await Task.Delay(150);
            }

            Console.WriteLine($"Fetched {fetchedObjects.Count} objects. Continue to deserialize them...");

            foreach (JObject document in fetchedObjects)
            {
                string type = document["type"].ToString();

                if (document["data"]["username"] == null) continue;
                User user = new User(document["data"]["username"].ToString());

                // [_id, type] already checked
                if (document["createdAt"] == null
                    || document["provider"] == null
                    || document["channel"] == null) continue;
                Activity activity = new Activity(DateTime.Parse(document["createdAt"].ToString()), document["provider"].ToString(), document["channel"].ToString(), type, document["_id"].ToString());

                switch (type)
                {
                    case "tip":
                        // [message] is nullable
                        if (document["data"]["tipId"] == null
                            || document["data"]["amount"] == null
                            || document["data"]["currency"] == null) continue;
                        
                        Tip tip = new Tip(document["data"]["tipId"].ToString(), document["data"]["amount"].ToObject<decimal>(), document["data"]["currency"].ToString(), (document["data"]["message"] == null ? null : document["data"]["message"].ToString()), activity, user);
                        this.fetchedDonations.Add(tip);
                        break;
                    case "subscriber":
                        // [message, User sent] is nullable
                        // [gifted] unsure if nullable - wont check
                        if (document["data"]["amount"] == null
                            || document["data"]["tier"] == null
                            || document["data"]["sender"] == null) continue;

                        Subscription subscription = new Subscription(document["data"]["amount"].ToObject<int>(), document["data"]["tier"].ToString(), (document["data"]["message"] == null ? null : document["data"]["message"].ToString()), (document["data"]["gifted"] == null ? false : document["data"]["gifted"].ToObject<bool>()), activity, user, (document["data"]["sender"] == null ? null : new User(document["data"]["sender"].ToString())));
                        this.fetchedSubscriptions.Add(subscription);
                        break;

                    case "cheer":
                        // [message] is nullable
                        if (document["data"]["amount"] == null) continue;

                        Cheer cheer = new Cheer(document["data"]["amount"].ToObject<int>(), (document["data"]["message"] == null ? null : document["data"]["message"].ToString()), activity, user);
                        this.fetchedCheers.Add(cheer);
                        break;

                    default:
                        continue;
                }
            }

            Console.WriteLine($"Fetched {fetchedDonations.Count} donations, {fetchedSubscriptions.Count} subscriptions and {fetchedCheers.Count} cheers in the last {goBack}(+1) days");
            done?.Invoke(true);
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
