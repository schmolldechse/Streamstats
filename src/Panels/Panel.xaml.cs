using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Streamstats.src.Service.Streamelements;
using Streamstats.src.Service;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Streamstats.src.Service.Objects.Types;
using Streamstats.src.Service.UI;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System;
using Streamstats.src.Service.Objects;

namespace Streamstats.src.Panels
{
    /// <summary>
    /// Interaction logic for Panel.xaml
    /// </summary>
    public partial class Panel : Window
    {
        /**
         * 1. missed tip
         * amount of missed tips
         */
        private Tip startToMiss_Tip;
        private int missedTips;

        /**
         * 1. missed subscription
         * amount of missed subscriptions
         */
        private Subscription startToMiss_Subscription;
        private int missedSubs;

        private bool ALERTS_PAUSED, ALERTS_MUTED;

        /**
         * {0} represents channelId
         */
        private readonly string STREAMELEMENTS_PAUSE_API = "https://api.streamelements.com/kappa/v3/overlays/{0}/action";
        private readonly string STREAMELEMENTS_SKIP_MUTE_API = "https://api.streamelements.com/kappa/v2/channels/{0}/socket";

        public Panel()
        {
            InitializeComponent();

            notificationCenter.Children.Add(new src.Notification.Notification(7, "#4CAF50", "#388E3C", "#f5f5f5", "Logged in", new Thickness(0, 15, 15, 15)));

            // Hide back to top buttons because there are no donations / subscriptions / cheers yet
            backToTop_Donations.Visibility = Visibility.Hidden;
            backToTop_Subscriptions.Visibility = Visibility.Hidden;

            // StreamElements
            App.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {App.config.jwtToken}");
            _ = App.se_service.fetchLatest(7, (done) =>
            {
                this.donation_Panel.Children.Remove(this.loading_Donations);
                this.donation_Panel.VerticalAlignment = VerticalAlignment.Stretch;
                this.loading_Donations = null;

                this.subscriptions_Panel.Children.Remove(this.loading_Subscriptions);
                this.subscriptions_Panel.VerticalAlignment = VerticalAlignment.Stretch;
                this.loading_Subscriptions = null;

                App.se_service.fetched = App.se_service.fetched.OrderBy(kvp => kvp.Key.createdAt).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                foreach (var kvp in App.se_service.fetched)
                {
                    if (kvp.Key == null) continue;
                    if (kvp.Value == null) continue;

                    switch (kvp.Value)
                    {
                        case Tip tip:
                            donation_Panel.Children.Insert(0, new TipGroupBox(tip, TipGroupBox.Category.NORMAL));
                            break;

                        case Subscription subscription:
                            subscriptions_Panel.Children.Insert(0, new SubscriptionGroupBox(subscription));
                            break;

                        case Cheer cheer:
                            donation_Panel.Children.Insert(0, new CheerGroupBox(cheer));
                            break;

                        default:
                            throw new ArgumentException("Key is invalid");
                    }
                }

                this.InitializeSocketEvents();
                this.loadPausedUnpausedState();
            });
        }

        private void InitializeSocketEvents()
        {
            // Something incoming [tip , subscription]
            App.se_service.client.On("event", (data) =>
            {
                Console.WriteLine($"Incoming event : {data}");
                handleIncoming(data.ToString());
            });

            // Receiving an update (pause/unpause)
            App.se_service.client.On("overlay:togglequeue", (data) =>
            {
                Console.WriteLine($"Received an overlay update (pause/unpause alerts) : {data}");

                bool value = (bool)JArray.Parse(data.ToString())[0];

                App.Current.Dispatcher.Invoke(() =>
                {
                    ALERTS_PAUSED = value;
                    switch (ALERTS_PAUSED)
                    {
                        case true:
                            pause_Button_Image.Dispatcher.Invoke(() => pause_Button_Image.Source = new BitmapImage(new Uri("../../Images/Play_Red.ico", UriKind.RelativeOrAbsolute)));
                            break;

                        case false:
                            pause_Button_Image.Dispatcher.Invoke(() => pause_Button_Image.Source = new BitmapImage(new Uri("../../Images/Pause.ico", UriKind.RelativeOrAbsolute)));
                            break;
                    }

                    pause_Button.Content = pause_Button_Image;
                });
            });

            // Receiving an update (mute/unmute)
            App.se_service.client.On("overlay:mute", (data) =>
            {
                Console.WriteLine($"Received an overlay update (mute/unmute alerts) : {data}");

                List<Dictionary<string, string>> jsonArray = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(data.ToString());
                bool value = Convert.ToBoolean(jsonArray[0]["muted"]);

                App.Current.Dispatcher.Invoke(() =>
                {
                    ALERTS_MUTED = value;
                    switch (ALERTS_MUTED)
                    {
                        case true:
                            mute_Button_Image.Dispatcher.Invoke(() => mute_Button_Image.Source = new BitmapImage(new Uri("../../Images/Muted.ico", UriKind.RelativeOrAbsolute)));
                            break;

                        case false:
                            mute_Button_Image.Dispatcher.Invoke(() => mute_Button_Image.Source = new BitmapImage(new Uri("../../Images/Unmuted.ico", UriKind.RelativeOrAbsolute)));
                            break;
                    }

                    mute_Button.Content = mute_Button_Image;
                });
            });



            App.se_service.client.On("connect_timeout", async (data) =>
            {
                this.notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Connection timed out", new Thickness(0, 15, 15, 15)));

                Console.WriteLine($"Connection timed out, trying to reconnect in 5 seconds");
                await Task.Delay(5000);


                await App.se_service.ConnectSocket(
                               async (success, data) =>
                               {
                                   if (!success)
                                   {
                                       Console.WriteLine("Connection refused. Redirecting to login panel");
                                       App.Current.Dispatcher.InvokeAsync(() => notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", $"Connection refused", new Thickness(0, 15, 15, 15))));
                                       await Task.Delay(5000);

                                       this.Hide();
                                       new Login().Show();
                                       return;
                                   }
                               });
            });
        }

        public void handleIncoming(string data)
        {
            JArray jArray = JArray.Parse(data);
            JObject document = (JObject)jArray.First;

            if (document["type"] == null) throw new ArgumentException("Type is missing or invalid.", nameof(document));

            App.se_service.fetchType(document, (result =>
            {
                if (result == null)
                {
                    Console.WriteLine("Could not fetch incoming data");
                    return;
                }

                App.Current.Dispatcher.Invoke(async () =>
                {
                    if (result is Subscription subscription)
                    {
                        GroupBox groupBox = new SubscriptionGroupBox(subscription);
                        this.subscriptions_Panel.Children.Add(groupBox);

                        if (scrollViewer_subscriberPanel.VerticalOffset > 20)
                        {
                            if (this.missedSubs == 0) startToMiss_Subscription = subscription;
                            missedSubs++;

                            backToTop_Subscriptions.Visibility = Visibility.Visible;
                            backToTop_Subscriptions_TextBlock.Text = missedSubs + " New Events";
                        }

                        Console.WriteLine($"Fetched incoming as subscription. Total : {App.se_service.fetched.Count}");
                    }
                    else if (result is Tip tip)
                    {
                        GroupBox groupBox = new TipGroupBox(tip, TipGroupBox.Category.NORMAL);
                        this.donation_Panel.Children.Insert(0, groupBox);

                        if (tip.amount >= this.highest().amount)
                        {
                            this.top_Donation.Children.Clear();
                            this.top_Donation.Children.Insert(0, new TipGroupBox(tip, TipGroupBox.Category.HIGHEST));
                        }

                        if (scrollViewer_donationPanel.VerticalOffset > 20)
                        {
                            if (this.missedTips == 0) startToMiss_Tip = tip;
                            missedTips++;

                            backToTop_Donations.Visibility = Visibility.Visible;
                            backToTop_Donations_TextBlock.Text = missedTips + " New Events";
                        }

                        Console.WriteLine($"Fetched incoming as tip. Total : {App.se_service.fetched.Count}");
                    }
                    else if (result is Cheer cheer)
                    {
                        GroupBox groupBox = new CheerGroupBox(cheer);
                        this.donation_Panel.Children.Insert(0, groupBox);

                        Console.WriteLine($"Fetched incoming as cheer. Total : {App.se_service.fetched.Count}");
                    }
                });
            }));
        }

        private Tip highest()
        {
            var tips = App.se_service.fetched.Values.OfType<Tip>();
            var highest = tips.OrderBy(tip => tip.amount).FirstOrDefault();
            return highest;
        }

        /**
         * Manages scrolling within ScrollViewer
         * If the user scrolls to the first missed subscription, count of 'missedSubs' will decrease & the next GroupBox (or the one which is before the current one) will be targetted
         * explaining 10/10
         */
        private void Scroll_Subscriber_Panel(object sender, ScrollChangedEventArgs e)
        {
            // Only interactions with mouse wheel are allowed
            if (!(e.ExtentHeightChange == 0 && e.ExtentWidthChange == 0)) return;

            if (this.startToMiss_Subscription == null) return;
            if (this.ById_SubscriberPanel(this.startToMiss_Subscription.activity.id) == null) return;

            GroupBox groupBox = this.ById_SubscriberPanel(this.startToMiss_Subscription.activity.id);

            GeneralTransform transform = groupBox.TransformToVisual(this.scrollViewer_subscriberPanel);
            Point topLeft = transform.Transform(new Point(0, 0));
            Point bottomRight = transform.Transform(new Point(groupBox.ActualWidth, groupBox.ActualHeight));

            bool visible = topLeft.Y >= 0 && bottomRight.Y <= this.scrollViewer_subscriberPanel.ActualHeight;
            if (visible)
            {
                if (this.missedSubs > 0)
                {
                    this.missedSubs--;
                    this.backToTop_Subscriptions_TextBlock.Text = this.missedSubs + " New Events";

                    if (this.missedSubs == 0) this.backToTop_Subscriptions.Visibility = Visibility.Hidden;
                }

                GroupBox next = Next_SubscriberPanel(groupBox);
                if (next == null) return;

                Subscription nextSub = next.Tag as Subscription;
                if (nextSub == null) return;

                this.startToMiss_Subscription = nextSub;
            }
        }


        /**
         * If the user clicks on the button, ScrollViewer will scroll to the 1. missed donation/ cheer. In addition, the GroupBox which is before the current one, will be targetted
         * explaining 10/10
         */
        private void backToTop_Subscribers_Click(object sender, RoutedEventArgs e)
        {
            if (!this.backToTop_Subscriptions.IsVisible) return;
            if (this.startToMiss_Subscription == null) return;
            if (this.ById_SubscriberPanel(this.startToMiss_Subscription.activity.id) == null) return;

            GroupBox groupBox = this.ById_SubscriberPanel(this.startToMiss_Subscription.activity.id);

            GeneralTransform transform = groupBox.TransformToVisual(this.scrollViewer_subscriberPanel);
            Point topLeft = transform.Transform(new Point(0, 0));
            Point bottomRight = transform.Transform(new Point(groupBox.ActualWidth, groupBox.ActualHeight));

            this.scrollViewer_subscriberPanel.ScrollToVerticalOffset(this.scrollViewer_subscriberPanel.VerticalOffset + (topLeft.Y < 0 ? -Math.Abs(topLeft.Y) : topLeft.Y));

            if (this.missedSubs > 0)
            {
                this.missedSubs--;
                this.backToTop_Subscriptions_TextBlock.Text = this.missedSubs + " New Events";

                if (this.missedSubs == 0) this.backToTop_Subscriptions.Visibility = Visibility.Hidden;
            }
        }

        /**
         * Manages scrolling within ScrollViewer
         * If the user scrolls to the first missed donation/ cheer, count of 'missedTips' will decrease & the next GroupBox (or the one which is before the current one) will be targetted
         * explaining 10/10
         */
        private void Scroll_Donation_Panel(object sender, ScrollChangedEventArgs e)
        {
            // Only interactions with mouse wheel are allowed
            if (!(e.ExtentHeightChange == 0 && e.ExtentWidthChange == 0)) return;

            if (this.startToMiss_Tip == null) return;
            if (this.ById_DonationPanel(this.startToMiss_Tip.activity.id) == null) return;

            GroupBox groupBox = this.ById_DonationPanel(this.startToMiss_Tip.activity.id);

            GeneralTransform transform = groupBox.TransformToVisual(this.scrollViewer_donationPanel);
            Point topLeft = transform.Transform(new Point(0, 0));
            Point bottomRight = transform.Transform(new Point(groupBox.ActualWidth, groupBox.ActualHeight));

            bool visible = topLeft.Y >= 0 && bottomRight.Y <= this.scrollViewer_donationPanel.ActualHeight;
            if (visible)
            {
                if (this.missedTips > 0)
                {
                    this.missedTips--;
                    this.backToTop_Donations_TextBlock.Text = this.missedTips + " New Events";

                    if (this.missedTips == 0) this.backToTop_Donations.Visibility = Visibility.Hidden;
                }

                GroupBox next = Next_DonationPanel(groupBox);
                if (next == null) return;

                Tip nextTip = next.Tag as Tip;
                if (nextTip == null) return;

                this.startToMiss_Tip = nextTip;
            }
        }


        /**
         * If the user clicks on the button, ScrollViewer will scroll to the 1. missed donation/ cheer. In addition, the GroupBox which is before the current one, will be targetted
         * explaining 10/10
         */
        private void backToTop_Donations_Click(object sender, RoutedEventArgs e)
        {
            if (!this.backToTop_Donations.IsVisible) return;
            if (this.startToMiss_Tip == null) return;
            if (this.ById_DonationPanel(this.startToMiss_Tip.activity.id) == null) return;

            GroupBox groupBox = this.ById_DonationPanel(this.startToMiss_Tip.activity.id);

            GeneralTransform transform = groupBox.TransformToVisual(this.scrollViewer_donationPanel);
            Point topLeft = transform.Transform(new Point(0, 0));
            Point bottomRight = transform.Transform(new Point(groupBox.ActualWidth, groupBox.ActualHeight));

            this.scrollViewer_donationPanel.ScrollToVerticalOffset(this.scrollViewer_donationPanel.VerticalOffset + (topLeft.Y < 0 ? -Math.Abs(topLeft.Y) : topLeft.Y));

            if (this.missedTips > 0)
            {
                this.missedTips--;
                this.backToTop_Donations_TextBlock.Text = this.missedTips + " New Events";

                if (this.missedTips == 0) this.backToTop_Donations.Visibility = Visibility.Hidden;
            }
        }

        /**
         * Search for groupbox by tip._id in subscriptions_Panel
         */
        private GroupBox ById_SubscriberPanel(string _id)
        {
            foreach (var child in this.subscriptions_Panel.Children)
            {
                if (child is GroupBox groupBox
                    && groupBox.Tag is Subscription tip
                    && tip.activity.id == _id)
                {
                    return groupBox;
                }
            }

            return null;
        }

        /**
         * Returns the groupbox before the provided one, or null in subscriptions_Panel
         */
        private GroupBox? Next_SubscriberPanel(GroupBox groupBox)
        {
            int currentIndex = this.subscriptions_Panel.Children.IndexOf(groupBox);
            if (currentIndex != -1 && currentIndex > 0) return this.subscriptions_Panel.Children[currentIndex - 1] as GroupBox;
            return null;
        }

        /**
         * Search for groupbox by tip._id in donation_Panel
         */
        private GroupBox ById_DonationPanel(string _id)
        {
            foreach (var child in this.donation_Panel.Children)
            {
                if (child is GroupBox groupBox
                    && (groupBox.Tag is Tip tip && tip.activity != null && tip.activity.id == _id
                || groupBox.Tag is Cheer cheer && cheer.activity != null && cheer.activity.id == _id))
                {
                    return groupBox;
                }
            }

            return null;
        }

        /**
         * Returns the groupbox before the provided one, or null in donation_Panel
         */
        private GroupBox? Next_DonationPanel(GroupBox groupBox)
        {
            int currentIndex = this.donation_Panel.Children.IndexOf(groupBox);
            if (currentIndex != -1 && currentIndex > 0) return this.donation_Panel.Children[currentIndex - 1] as GroupBox;
            return null;
        }

        /**
         * Pause / unpause alerts button
         */
        private void Pause_Button_Click(object sender, RoutedEventArgs e)
        {
            ALERTS_PAUSED = !ALERTS_PAUSED;
            switch (ALERTS_PAUSED)
            {
                case true:
                    pause_Button_Image.Source = new BitmapImage(new Uri("../../Images/Play_Red.ico", UriKind.RelativeOrAbsolute));
                    break;

                case false:
                    pause_Button_Image.Source = new BitmapImage(new Uri("../../Images/Pause.ico", UriKind.RelativeOrAbsolute));
                    break;
            }

            pause_Button.Content = pause_Button_Image;

            Task.Run(async () =>
            {
                string action = ALERTS_PAUSED ? "pause" : "unpause";
                using StringContent jsonContent = new StringContent(
                    $"{{ \"action\" : \"{action}\" }}",
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage responseMessage = await App.httpClient.PutAsync(string.Format(STREAMELEMENTS_PAUSE_API, App.se_service.channelId), jsonContent);
                var jsonResponse = await responseMessage.Content.ReadAsStringAsync();

                Console.WriteLine($"Sent http request : {jsonResponse}");
            });
        }

        /**
         * Skip alerts button
         */
        private void Skip_Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                using StringContent jsonContent = new StringContent(
                    "{\"event\": \"event:skip\", \"data\": {}}",
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage responseMessage = await App.httpClient.PostAsync(string.Format(STREAMELEMENTS_SKIP_MUTE_API, App.se_service.channelId), jsonContent);
                var jsonResponse = await responseMessage.Content.ReadAsStringAsync();

                Console.WriteLine($"Sent http request : {jsonResponse}");
            });
        }

        /**
         * Mute / unmute alerts button
         */
        private void Mute_Button_Click(object sender, RoutedEventArgs e)
        {
            ALERTS_MUTED = !ALERTS_MUTED;
            switch (ALERTS_MUTED)
            {
                case true:
                    mute_Button_Image.Source = new BitmapImage(new Uri("../../Images/Muted.ico", UriKind.RelativeOrAbsolute));
                    break;

                case false:
                    mute_Button_Image.Source = new BitmapImage(new Uri("../../Images/Unmuted.ico", UriKind.RelativeOrAbsolute));
                    break;
            }

            mute_Button.Content = mute_Button_Image;

            Task.Run(async () =>
            {
                using StringContent jsonContent = new StringContent(
                    $"{{\"event\": \"overlay:mute\", \"data\": {{ \"muted\": \"{ALERTS_MUTED}\" }} }}",
                    Encoding.UTF8,
                    "application/json");

                using HttpResponseMessage responseMessage = await App.httpClient.PostAsync(string.Format(STREAMELEMENTS_SKIP_MUTE_API, App.se_service.channelId), jsonContent);
                var jsonResponse = await responseMessage.Content.ReadAsStringAsync();

                Console.WriteLine($"Sent http request : {jsonResponse}");
            });
        }


        /**
         * Loads current state of donations [paused / unpaused, muted / unmuted]
         */
        private void loadPausedUnpausedState()
        {
            Task.Run(async () =>
            {
                var response = await App.httpClient.GetAsync(string.Format(STREAMELEMENTS_PAUSE_API, App.se_service.channelId));
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonObject = JsonConvert.DeserializeObject<JObject>(responseContent);
                    Console.WriteLine($"Received state : {responseContent}");

                    ALERTS_PAUSED = (bool)jsonObject["paused"];
                    ALERTS_MUTED = (bool)jsonObject["muted"];

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        pause_Button_Image.Source = new BitmapImage(new Uri("../../Images/" + (this.ALERTS_PAUSED ? "Play_Red.ico" : "Pause.ico"), UriKind.RelativeOrAbsolute));
                        pause_Button.Content = pause_Button_Image;

                        mute_Button_Image.Source = new BitmapImage(new Uri("../../Images/" + (this.ALERTS_MUTED ? "Muted.ico" : "Unmuted.ico"), UriKind.RelativeOrAbsolute));
                        mute_Button.Content = mute_Button_Image;
                    });
                }
                else notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Could not load state", new Thickness(0, 15, 15, 15)));
            });
        }
    }
}
