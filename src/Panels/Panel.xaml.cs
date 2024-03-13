using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Streamstats.src.Service.Objects.Types;
using Streamstats.src.Service.UI;
using Streamstats.src.UI.Buttons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

        /**
         * {0} represents channelId
         */
        private readonly string OVERLAY_API = "https://api.streamelements.com/kappa/v3/overlays/{0}/action";

        /**
         * Border brushes for searchbox
         */
        private SolidColorBrush GRAY = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333742"));
        private SolidColorBrush PURPLE = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6D28D9"));

        public Panel()
        {
            InitializeComponent();

            notificationCenter.Children.Add(new src.Notification.Notification(7, "#4CAF50", "#388E3C", "#f5f5f5", "Logged in", new Thickness(0, 15, 15, 15)));

            // Hide back to top buttons because there are no donations / subscriptions / cheers yet
            backToTop_Donations.Visibility = Visibility.Hidden;
            backToTop_Subscriptions.Visibility = Visibility.Hidden;

            // StreamElements
            App.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {App.config.jwtToken}");
            _ = App.se_service.fetchLatest(5, (done) =>
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
                            GroupBox tipGroupBox = new TipGroupBox(tip, TipGroupBox.Category.NORMAL);
                            this.donation_Panel.Children.Insert(0, tipGroupBox);
                            break;

                        case Subscription subscription:
                            GroupBox subscriptionGroupBox = new SubscriptionGroupBox(subscription);
                            this.subscriptions_Panel.Children.Insert(0, subscriptionGroupBox);
                            break;

                        case Cheer cheer:
                            GroupBox cheerGroupBox = new CheerGroupBox(cheer);
                            this.donation_Panel.Children.Insert(0, new CheerGroupBox(cheer));
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
                    this.pause_Button.Tag = (bool) value;

                    Image? pauseImage = pause_Button.Content as Image;
                    pauseImage.Source = new BitmapImage(new Uri("../../Images/" + ((bool)this.pause_Button.Tag ? "Play_Red.ico" : "Pause.ico"), UriKind.RelativeOrAbsolute));
                    pause_Button.Content = pauseImage;
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
                    this.mute_Button.Tag = (bool)value;

                    Image? muteImage = mute_Button.Content as Image;
                    muteImage.Source = new BitmapImage(new Uri("../../Images/" + ((bool)this.mute_Button.Tag ? "Muted.ico" : "Unmuted.ico"), UriKind.RelativeOrAbsolute));
                    mute_Button.Content = muteImage;
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
                        bool match = this.IsMatch(new string[] { subscription.user.username, subscription.message });

                        GroupBox groupBox = new SubscriptionGroupBox(subscription);
                        groupBox.Visibility = (this.searchBox.Text.Length > 0 && match) ? Visibility.Visible : Visibility.Collapsed;
                        this.subscriptions_Panel.Children.Add(groupBox);

                        if (this.scrollViewer_subscriberPanel.VerticalOffset > 20)
                        {
                            if (this.missedSubs == 0) this.startToMiss_Subscription = subscription;
                            missedSubs++;

                            this.backToTop_Subscriptions.Visibility = Visibility.Visible;
                            this.backToTop_Subscriptions_TextBlock.Text = this.missedSubs + " New Events";
                        }

                        Console.WriteLine($"Fetched incoming as subscription. Total : {App.se_service.fetched.Count}");
                    }
                    else if (result is Tip tip)
                    {
                        bool match = this.IsMatch(new string[] { tip.user.username, tip.message });

                        GroupBox groupBox = new TipGroupBox(tip, TipGroupBox.Category.NORMAL);
                        groupBox.Visibility = (this.searchBox.Text.Length > 0 && match) ? Visibility.Visible : Visibility.Collapsed;
                        this.donation_Panel.Children.Insert(0, groupBox);

                        if (tip.amount >= this.highest().amount)
                        {
                            this.top_Donation.Children.Clear();
                            this.top_Donation.Children.Insert(0, new TipGroupBox(tip, TipGroupBox.Category.HIGHEST));
                        }

                        if (this.scrollViewer_donationPanel.VerticalOffset > 20)
                        {
                            if (this.missedTips == 0) this.startToMiss_Tip = tip;
                            this.missedTips++;

                            this.backToTop_Donations.Visibility = Visibility.Visible;
                            this.backToTop_Donations_TextBlock.Text = this.missedTips + " New Events";
                        }

                        Console.WriteLine($"Fetched incoming as tip. Total : {App.se_service.fetched.Count}");
                    }
                    else if (result is Cheer cheer)
                    {
                        bool match = this.IsMatch(new string[] { cheer.user.username, cheer.message });

                        GroupBox groupBox = new CheerGroupBox(cheer);
                        groupBox.Visibility = (this.searchBox.Text.Length > 0 && match) ? Visibility.Visible : Visibility.Collapsed;
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
         * Loads current state of donations [paused / unpaused, muted / unmuted]
         */
        private void loadPausedUnpausedState()
        {
            Task.Run(async () =>
            {
                var response = await App.httpClient.GetAsync(string.Format(this.OVERLAY_API, App.se_service.channelId));
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonObject = JsonConvert.DeserializeObject<JObject>(responseContent);
                    Console.WriteLine($"Received state : {responseContent}");

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        this.pause_Button.Tag = (bool)jsonObject["paused"];
                        this.mute_Button.Tag = (bool)jsonObject["muted"];

                        Image? pauseImage = pause_Button.Content as Image;
                        pauseImage.Source = new BitmapImage(new Uri("../../Images/" + ((bool) this.pause_Button.Tag ? "Play_Red.ico" : "Pause.ico"), UriKind.RelativeOrAbsolute));
                        pause_Button.Content = pauseImage;

                        Image? muteImage = mute_Button.Content as Image;
                        muteImage.Source = new BitmapImage(new Uri("../../Images/" + ((bool) this.mute_Button.Tag ? "Muted.ico" : "Unmuted.ico"), UriKind.RelativeOrAbsolute));
                        mute_Button.Content = muteImage;
                    });
                }
                else notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Could not load state", new Thickness(0, 15, 15, 15)));
            });
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            this.search_Border.BorderBrush = textBox.Text.Length > 0 ? PURPLE : GRAY;

            foreach (GroupBox groupBox in this.donation_Panel.Children)
            {
                var document = groupBox.Tag;
                if (document is Tip tip)
                {
                    bool match = this.IsMatch(new string[] { tip.user.username, tip.message });
                    groupBox.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                }

                if (document is Subscription subscription)
                {
                    bool match = this.IsMatch(new string[] { subscription.user.username, subscription.message });
                    groupBox.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                }

                if (document is Cheer cheer)
                {
                    bool match = this.IsMatch(new string[] { cheer.user.username, cheer.message });
                    groupBox.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            this.scrollViewer_donationPanel.UpdateLayout();
        }

        private void searchBox_MouseEnter(object sender, MouseEventArgs e)
        {
            this.search_Border.BorderBrush = PURPLE;
        }

        private void searchBox_MouseLeave(object sender, MouseEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            this.search_Border.BorderBrush = textBox.Text.Length > 0 ? PURPLE : GRAY;
        }

        private bool IsMatch(string[] check)
        {
            if (check[0]?.ToLower().Contains(this.searchBox.Text.ToLower()) ?? false) return true;
            if (check.Length > 1 && check[1].ToLower().Contains(this.searchBox.Text.ToLower())) return true;
            return false;
        }
    }
}
