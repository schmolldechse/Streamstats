using Streamstats.src.Service.Streamelements;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Streamstats.src.Service
{
    public class Donation_GroupBox
    {

        private readonly string STREAMELEMENTS_REPLAY_API = "https://api.streamelements.com/kappa/v2/activities/{0}/{1}/replay";

        private Donation _donation;

        private DispatcherTimer timer;

        private TextBlock? timeAgoBlock;

        private Type type;

        private Color gold = Color.FromRgb(90, 83, 54);
        private Color gray = Color.FromRgb(33, 41, 54);

        public Donation_GroupBox(Donation donation, Type type)
        {
            this._donation = donation;
            this.type = type;

            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(1);
            this.timer.Tick += (sender, e) =>
            {
                if (timeAgoBlock != null) timeAgoBlock.Text = timeDifference(_donation.createdAt);
            };
            this.timer.Start();
        }

        public GroupBox create()
        {
            StackPanel stackPanel = new StackPanel();

            //Whole grid of groupbox
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            //TOP LEFT
            StackPanel topLeft = new StackPanel();
            topLeft.Orientation = Orientation.Horizontal;
            topLeft.Margin = new Thickness(0, 12, 0, 5);
            Grid.SetRow(topLeft, 0);

            //TOP LEFT | AMOUNT
            TextBlock amount_Outer = new TextBlock();
            amount_Outer.Margin = new Thickness(5, 0, 0, 0);

            Border amount_Border = new Border();
            amount_Border.BorderBrush = new SolidColorBrush(type == Type.HIGHEST ? gold : gray);
            amount_Border.Background = new SolidColorBrush(type == Type.HIGHEST ? gold : gray);
            amount_Border.CornerRadius = new CornerRadius(10);

            TextBlock amount_Inner = new TextBlock();
            amount_Inner.Text = formatCurrency(_donation.data.amount, _donation.data.currency);
            amount_Inner.FontWeight = FontWeights.Bold;
            amount_Inner.Foreground = Brushes.FloralWhite;
            amount_Inner.Margin = new Thickness(11, 4, 11, 4);
            //amount_Inner.Effect = new DropShadowEffect() { Color = Colors.White, Direction = 0, ShadowDepth = 0, Opacity = 1 };

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
            timeAgo.Margin = new Thickness(0, 10, 4, 0); //old : 0 16 10 0
            timeAgo.Text = timeDifference(_donation.createdAt);
            timeAgo.Foreground = Brushes.Gray;
            timeAgo.FontWeight = FontWeights.Medium;
            timeAgo.HorizontalAlignment = HorizontalAlignment.Right;
            timeAgo.VerticalAlignment = VerticalAlignment.Center;

            timeAgoBlock = timeAgo;
            //Grid.SetRow(timeAgo, 0);

            //REPLAY BUTTON
            Button replay = new Button();
            replay.Margin = new Thickness(0, 0, 5, 5);
            replay.HorizontalAlignment = HorizontalAlignment.Right;
            replay.VerticalAlignment = VerticalAlignment.Bottom;
            replay.Background = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#030713"));
            replay.BorderThickness = new Thickness(0);
            replay.Style = (Style)App.Current.FindResource("replayButton");
            Grid.SetRow(replay, 1);
            Grid.SetColumn(replay, 1);

            //REPLAY BUTTON | IMAGE
            Image replayIcon = new Image();
            replayIcon.Source = new BitmapImage(new Uri("../../Images/Replay.ico", UriKind.RelativeOrAbsolute));
            replayIcon.Height = 20;
            replayIcon.Width = 20;

            replay.Content = replayIcon;
            replay.Click += Replay_Click;

            //MESSAGE PANEL
            WrapPanel? messagePanel = null;
            if (_donation.data.message.Length > 1)
            {
                messagePanel = new WrapPanel();
                messagePanel.Margin = new Thickness(3, 0, 3, 5);
                Grid.SetRow(messagePanel, 1);

                //MESSAGE PANEL | MESSAGE
                RichTextBox message = new RichTextBox();

                Paragraph paragraph = new Paragraph();
                paragraph.Foreground = Brushes.FloralWhite;
                foreach (string segment in _donation.data.message.Split(' '))
                {
                    if (Uri.IsWellFormedUriString(segment, UriKind.Absolute))
                    {
                        Hyperlink hyperlink = new Hyperlink(new Run(segment));
                        hyperlink.NavigateUri = new Uri(segment);
                        hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(105, 60, 153));
                        //TODO: hyperlink.RequestNavigate += Hyperlink_RequestNavigate;

                        paragraph.Inlines.Add(hyperlink);
                        paragraph.Inlines.Add(new Run(" "));
                    }
                    else
                    {
                        paragraph.Inlines.Add(new Run(segment + " "));
                    }
                }

                FlowDocument flowDocument = new FlowDocument();
                flowDocument.Blocks.Add(paragraph);
                message.Document = flowDocument;
                message.IsReadOnly = true;
                message.Background = new SolidColorBrush(Color.FromRgb(3, 7, 19));
                message.BorderThickness = new Thickness(0);
                message.FontWeight = FontWeights.Medium;
                messagePanel.Children.Add(message);
            }

            //ADD TO GRID
            grid.Children.Add(topLeft);
            grid.Children.Add(timeAgo);
            //grid.Children.Add(replay);
            if (messagePanel != null) grid.Children.Add(messagePanel);

            //ADD GRID TO STACK PANEL
            stackPanel.Children.Add(grid);

            GroupBox groupBox = new GroupBox();
            groupBox.Content = stackPanel;
            groupBox.Margin = new Thickness((type == Type.NORMAL ? 0 : 7), 0, (type == Type.NORMAL ? 0 : 10), (type == Type.NORMAL ? 4 : 6) );
            //groupBox.Style = (Style) App.Current.FindResource( type == Type.NORMAL ? "donation" : "highestDonation" );
            groupBox.Style = (Style) App.Current.FindResource("groupBoxWithBorder");
            groupBox.Tag = _donation;
            groupBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString((type == Type.NORMAL ? "#1E222E" : "#5A5336")));
            groupBox.BorderThickness = new Thickness((type == Type.NORMAL ? 0.4 : 0.7));

            return groupBox;
        }

        private void Replay_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                using StringContent jsonContent = new StringContent("", Encoding.UTF8, "application/json");

                using HttpResponseMessage responseMessage = await App.httpClient.PostAsync(string.Format(STREAMELEMENTS_REPLAY_API, App.se_service.channelId, _donation.data.activityId), jsonContent);
                var jsonResponse = await responseMessage.Content.ReadAsStringAsync();

                Console.WriteLine($"Sent http request : {jsonResponse}");
            });
        }

        private string formatCurrency(decimal amount, string currency)
        {
            string a = amount % 1 == 0 ? $"{amount}" : $"{amount: 0.00}";
            string b = currency switch
            {
                "EUR" => a + " €",
                "USD" => "$ " + a,
                _ => a + currency
            };
            return b;
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

        public enum Type
        {
            NORMAL,
            HIGHEST,
        }
    }
}
