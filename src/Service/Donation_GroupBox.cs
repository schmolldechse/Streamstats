using Streamstats.src.Service.Streamelements;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
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

            //Whole grid of groupbox
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            //TOP LEFT
            StackPanel topLeft = new StackPanel();
            topLeft.Orientation = Orientation.Horizontal;
            topLeft.Margin = new Thickness(0, 12, 0, 5);
            Grid.SetRow(topLeft, 0);

            //TOP LEFT | AMOUNT
            TextBlock amount_Outer = new TextBlock();
            amount_Outer.Margin = new Thickness(5, 0, 0, 0);

            Border amount_Border = new Border();
            amount_Border.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 41, 54));
            amount_Border.Background = new SolidColorBrush(Color.FromRgb(33, 41, 54));
            amount_Border.CornerRadius = new CornerRadius(10);

            TextBlock amount_Inner = new TextBlock();
            amount_Inner.Text = _donation.data.amount + " " + currency(_donation.data.currency);
            amount_Inner.Foreground = Brushes.FloralWhite;
            amount_Inner.Margin = new Thickness(15, 5, 15, 5);
            amount_Inner.Effect = new DropShadowEffect() { Color = Colors.White, Direction = 0, ShadowDepth = 0, Opacity = 1 };

            amount_Border.Child = amount_Inner;
            amount_Outer.Inlines.Add(amount_Border);

            //TOP LEFT | USERNAME
            TextBlock username = new TextBlock();
            username.Margin = new Thickness(10, 4, 0, 0);
            username.Text = _donation.data.username;
            username.Foreground = new SolidColorBrush(Color.FromRgb(91, 187, 165));
            username.FontWeight = FontWeights.Bold;
            username.Effect = new DropShadowEffect() { Color = Color.FromRgb(91, 187, 165), Direction = 0, ShadowDepth = 0, Opacity = 1 };

            //ADD TO TOP LEFT
            topLeft.Children.Add(amount_Outer);
            topLeft.Children.Add(username);

            //DONATION ... AGO
            TextBlock timeAgo = new TextBlock();
            timeAgo.Margin = new Thickness(0, 16, 10, 0);
            timeAgo.Text = timeDifference(_donation.createdAt);
            timeAgo.Foreground = Brushes.Gray;
            timeAgo.FontWeight = FontWeights.Medium;
            timeAgo.HorizontalAlignment = HorizontalAlignment.Right;
            timeAgo.VerticalAlignment = VerticalAlignment.Top;
            //Grid.SetRow(timeAgo, 0);

            //MESSAGE PANEL
            WrapPanel messagePanel = new WrapPanel();
            messagePanel.Margin = new Thickness(10, 0, 10, 5);
            Grid.SetRow(messagePanel, 1);

            //MESSAGE PANEL | MESSAGE
            TextBox message = new TextBox();
            message.TextWrapping = TextWrapping.Wrap;
            message.Text = _donation.data.message;
            message.Foreground = Brushes.FloralWhite;
            message.Background = new SolidColorBrush(Color.FromRgb(3, 7, 19));
            message.BorderThickness = new Thickness(0);
            messagePanel.Children.Add(message);

            //ADD TO GRID
            grid.Children.Add(topLeft);
            grid.Children.Add(timeAgo);
            grid.Children.Add(messagePanel);

            //ADD TO STACK PANEL
            stackPanel.Children.Add(grid);

            GroupBox groupBox = new GroupBox();
            groupBox.Content = stackPanel;
            //TODO
            //groupBox.Style = (Style) App.Current.FindResource("groupBoxStyle");
            //groupBox.Style = (Style)Application.Current.Resources["groupBoxStyle"];

            return this;
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
