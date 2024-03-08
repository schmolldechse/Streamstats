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
using System.IO.Packaging;

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
                fetchType(document);
            }

            Console.WriteLine($"Fetched {fetchedDonations.Count} donations, {fetchedSubscriptions.Count} subscriptions and {fetchedCheers.Count} cheers in the last {goBack}(+1) days");
            done?.Invoke(true);
        }

        public void fetchType(JObject document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            string type = GetOrDefault<string>(document, "type");
            if (string.IsNullOrEmpty(type)) throw new ArgumentNullException("Type is missing or invalid.", nameof(document));

            string username = GetOrDefault<string>(document["data"], "username");
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("Username is missing or invalid.", nameof(document));
            User user = new User(username);

            DateTime createdAt = GetOrDefault<DateTime>(document, "createdAt");
            string provider = GetOrDefault<string>(document, "provider");
            string channel = GetOrDefault<string>(document, "channel");
            if (string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(channel)) throw new ArgumentException("Provider or channel is missing or invalid.", nameof(document));
            Activity activity = new Activity(createdAt, provider, channel, type, GetOrDefault<string>(document, "_id"));


            decimal amount = GetOrDefault<decimal>(document["data"], "amount");
            string message = GetOrDefault<string>(document["data"], "message");

            switch (type)
            {
                case "tip":
                    string tipId = GetOrDefault<string>(document["data"], "tipId");
                    string currency = GetOrDefault<string>(document["data"], "currency");
                    if (string.IsNullOrEmpty(tipId) || string.IsNullOrEmpty(currency)) throw new ArgumentException("TipId or Amount is missing or invalid.", nameof(document));

                    Tip tip = new Tip(tipId, amount, currency, message, activity, user);
                    this.fetchedDonations.Add(tip); 
                    break;

                case "subscriber":
                    string tier = GetOrDefault<string>(document["data"], "tier");
                    if (string.IsNullOrEmpty(tier)) throw new ArgumentException("Tier is missing or invalid.", nameof(document));

                    bool gifted = GetOrDefault<bool>(document["data"], "gifted");

                    User sender = null;
                    if (document["data"]["sender"] != null)
                    {
                        sender = new User(GetOrDefault<string>(document["data"], "sender"));
                    }

                    Subscription subscription = new Subscription((int) amount, tier, message, gifted, activity, user, (sender != null ? sender : null));
                    this.fetchedSubscriptions.Add(subscription);
                    break;

                case "cheer":
                    Cheer cheer = new Cheer((int) amount, message, activity, user);
                    this.fetchedCheers.Add(cheer);
                    break;

                default:
                    throw new ArgumentException("Unknown type.", nameof(document));
            }
        }

        private T GetOrDefault<T>(JToken token, string property)
        {
            if (token == null || token[property] == null) return default;
            return token[property].ToObject<T>();
        }
    }
}
