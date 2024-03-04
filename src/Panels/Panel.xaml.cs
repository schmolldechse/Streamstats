using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Streamstats.src.Service.Streamelements;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using Streamstats.src.Service;
using System.Diagnostics;

namespace Streamstats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Panel : Window
    {

        private GroupBox topDonation_GroupBox;

        public Panel()
        {
            InitializeComponent();

            //StreamElements
            App.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {App.config.jwtToken}");
            if (App.se_service.CONNECTED) App.se_service.fetchLatestTips();

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

            App.se_service.client.On("event", (data) =>
            {
                Console.WriteLine($"Received a new donation {data}");
                handleIncomingDonation(data.ToString());
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
            });
        }

        private Donation highest()
        {
            return App.se_service.donations.OrderByDescending(donation => donation.data.amount).FirstOrDefault();
        }
    }
}