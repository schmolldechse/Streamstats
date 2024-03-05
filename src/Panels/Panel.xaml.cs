using Streamstats.src.Service.Streamelements;
using Streamstats.src.Service;
using System;
using System.Collections.Generic;
using System.Linq;
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
using Newtonsoft.Json.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Effects;
using Streamstats.src.Notification;
using System.Net.Http;
using Newtonsoft.Json;

namespace Streamstats.src.Panels
{
    /// <summary>
    /// Interaction logic for Panel.xaml
    /// </summary>
    public partial class Panel : Window
    {

        private Donation startToMiss_Donation;
        private int missedDonations;

        private bool ALERTS_PAUSED;

        //private double previousY_Donation;

        /**
         * {0} represents channelId
         */
        private readonly string STREAMELEMENTS_PAUSE_API = "https://api.streamelements.com/kappa/v3/overlays/{0}/action";

        public Panel()
        {
            InitializeComponent();

            notificationCenter.Children.Add(new src.Notification.Notification(7, "#4CAF50", "#388E3C", "#f5f5f5", "Logged in"));


            // Hide back to top donations because there are no donations / subscriptions yet
            backToTop_Donations.Visibility = Visibility.Hidden;
            backToTop_Subscriptions.Visibility = Visibility.Hidden;

            // StreamElements
            App.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {App.config.jwtToken}");
            if (App.se_service.CONNECTED) App.se_service.fetchLatestTips();

            this.loadPausedUnpausedState();

            do
            {
                if (App.se_service.FETCHED_DONATIONS) break;
            } while (!App.se_service.FETCHED_DONATIONS);
            App.se_service.donations.Sort((donation1, donation2) => donation1.createdAt.CompareTo(donation2.createdAt));

            foreach (Donation donation in App.se_service.donations)
            {
                GroupBox groupBox = new Donation_GroupBox(donation, Donation_GroupBox.Type.NORMAL).create();
                donation_Panel.Children.Insert(0, groupBox);
            }

            // Incoming donations
            App.se_service.client.On("event", (data) =>
            {
                Console.WriteLine($"Received a new donation : {data}");
                handleIncomingDonation(data.ToString());
            });

            // Receiving an update (pause/unpause)
            App.se_service.client.On("overlay:togglequeue", (data) =>
            {
                Console.WriteLine($"Received an overlay update (pause/unpause) : {data}");

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
                    pause_Button_Image.Height = 15;
                    pause_Button_Image.Width = 15;

                    pause_Button.Content = pause_Button_Image;
                });
            });

            // Receiving an update (mute/unmute)
            App.se_service.client.On("overlay:mute", (data) =>
            {
                Console.WriteLine($"Received an overlay update (mute/unmute) : {data}");
            });
        }

        public void handleIncomingDonation(string data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                JArray jArray = JArray.Parse(data);
                JObject donation = (JObject)jArray.First;

                Donation fetched = App.se_service.fetchDonation(donation);
                App.se_service.donations.Add(fetched);

                GroupBox groupBox = new Donation_GroupBox(fetched, Donation_GroupBox.Type.NORMAL).create();
                donation_Panel.Children.Insert(0, groupBox);

                if (fetched.data.amount >= highest().data.amount)
                {
                    top_Donation.Children.Clear();
                    top_Donation.Children.Insert(0, new Donation_GroupBox(fetched, Donation_GroupBox.Type.HIGHEST).create());
                }

                App.se_service.donations.Add(fetched);

                if (scrollViewer_donationPanel.VerticalOffset > 20)
                {
                    if (missedDonations == 0) startToMiss_Donation = fetched;
                    missedDonations++;

                    backToTop_Donations.Visibility = Visibility.Visible;
                    backToTop_Donations.Content = "⮝ " + missedDonations + " New Events";
                }
            });
        }

        private Donation highest()
        {
            return App.se_service.donations.OrderByDescending(donation => donation.data.amount).FirstOrDefault();
        }

        /**
         * fix
        private void Scroll_Donations(object sender, ScrollChangedEventArgs e)
        {
            if (startToMiss_Donation == null) return;
            if (byId(startToMiss_Donation._id) == null) return; 

            GroupBox groupBox = byId(startToMiss_Donation._id);

            GeneralTransform transform = groupBox.TransformToAncestor(scrollViewer_donationPanel);
            Point position = transform.Transform(new Point(0, 0));

            if (Math.Sign(position.Y - previousY_Donation) != 0
                && backToTop_Donations.IsVisible)
            {
                missedDonations--;
                backToTop_Donations.Content = "⮝ " + missedDonations + " New Events";

                if (missedDonations == 0) backToTop_Donations.Visibility = Visibility.Hidden;

                if (donation_Panel.Children.IndexOf(groupBox) > 0)
                {
                    GroupBox newer_GroupBox = donation_Panel.Children[donation_Panel.Children.IndexOf(groupBox) - 1] as GroupBox;
                    if (newer_GroupBox == null) return;

                    Donation newer_Donation = newer_GroupBox.Tag as Donation;
                    if (newer_Donation == null) return;

                    startToMiss_Donation = newer_Donation;
                }
            }

            previousY_Donation = position.Y;
        }
        */

        private async void backToTop_Donations_Click(object sender, RoutedEventArgs e)
        {
            if (!backToTop_Donations.IsVisible) return;
            if (startToMiss_Donation == null) return;
            if (byId(startToMiss_Donation._id) == null) return;

            GroupBox groupBox = byId(startToMiss_Donation._id);

            GeneralTransform transform = groupBox.TransformToAncestor(scrollViewer_donationPanel);
            Point position = transform.Transform(new Point(0, 0));

            scrollViewer_donationPanel.ScrollToVerticalOffset(scrollViewer_donationPanel.VerticalOffset + (position.Y < 0 ? -Math.Abs(position.Y) : position.Y));

            missedDonations = 0;
            backToTop_Donations.Visibility = Visibility.Hidden;
        }

        /**
         * Search for groupbox by donation._id
         */
        private GroupBox byId(string _id) 
        {
            foreach (var child in donation_Panel.Children)
            {
                if (child is GroupBox groupBox 
                    && groupBox.Tag is Donation donation
                    && donation._id == _id)
                {
                    return groupBox;
                }
            }

            return null;
        }

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

            pause_Button_Image.Height = 15;
            pause_Button_Image.Width = 15;

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
         * Loads current state of donations [paused, unpause]
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
                    switch (ALERTS_PAUSED)
                    {
                        case true:
                            pause_Button_Image.Source = new BitmapImage(new Uri("../../Images/Play_Red.ico", UriKind.RelativeOrAbsolute));
                            break;

                        case false:
                            pause_Button_Image.Source = new BitmapImage(new Uri("../../Images/Pause.ico", UriKind.RelativeOrAbsolute));
                            break;
                    }

                    pause_Button_Image.Height = 15;
                    pause_Button_Image.Width = 15;

                    pause_Button.Content = pause_Button_Image;
                } else notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Could not load state (paused?)"));
            });
        }
    }
}
