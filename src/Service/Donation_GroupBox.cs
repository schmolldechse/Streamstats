using Streamstats.src.Service.Streamelements;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Streamstats.src.Service
{
    public class Donation_GroupBox : GroupBox
    {

        private Donation _donation;

        private DispatcherTimer timer;

        public Donation_GroupBox(Donation donation)
        {
            this._donation = donation;

            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(1);
            this.timer.Tick += (sender, e) =>
            {

            };
        }

        public GroupBox create()
        {
            StackPanel stackPanel = new StackPanel();



            TextBlock amount = new TextBlock();
            amount.Text = _donation.data.amount + currency(_donation.data.currency);
            amount.Foreground = Brushes.FloralWhite;

            Border amountBorder = new Border();
            amountBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 41, 54));
            amountBorder.BorderThickness = new Thickness(2);
            amountBorder.Background = new SolidColorBrush(Color.FromRgb(33, 41, 54));
            amountBorder.Child = amount;

            stackPanel.Children.Add(amountBorder);

            GroupBox groupBox = new GroupBox();
            groupBox.Content = stackPanel;

            return groupBox;
        }

        private string currency(string currencyCode)
        {
            CultureInfo cultureInfo = CultureInfo.GetCultureInfoByIetfLanguageTag("de-DE");
            RegionInfo regionInfo = new RegionInfo(cultureInfo.Name);
            return regionInfo.CurrencySymbol;
        }

        private string timeDifference(DateTime past)
        {
            TimeSpan difference = DateTime.Now - past.AddHours(1);

            if (difference.TotalSeconds <= 60)
            {
                return $"{(int)difference.TotalSeconds} s";
            }
            else if (difference.TotalMinutes <= 60)
            {
                return $"{(int)difference.TotalMinutes} m";
            }
            else if (difference.TotalHours <= 24)
            {
                return $"{(int)difference.TotalHours} h";
            }
            else
            {
                return $"{(int)difference.TotalDays} d";
            }
        }
    }
}
