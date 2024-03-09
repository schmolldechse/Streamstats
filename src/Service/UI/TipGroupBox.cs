using Accessibility;
using Streamstats.src.Service.Objects.Types;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Streamstats.src.Service.UI
{
    public class TipGroupBox : GroupBox
    {

        private readonly string STREAMELEMENTS_REPLAY_API = "https://api.streamelements.com/kappa/v2/activities/{0}/{1}/replay";

        public Tip _tip;
        public Category _category;

        private DispatcherTimer timer;

        private TextBlock timeAgo_TextBlock;
        private Image replay_Image;

        private Color gold = Color.FromRgb(90, 83, 54);
        private Color gray = Color.FromRgb(33, 41, 54);

        public TipGroupBox(Tip tip, Category category) 
        {
            this._tip = tip;
            this._category = category;

            // Initializing timer
            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(1);
            this.timer.Tick += (sender, e) =>
            {
                if (this.timeAgo_TextBlock != null) this.timeAgo_TextBlock.Text = timeDifference(_tip.activity.createdAt);
            };
            this.timer.Start();

            // Initializing groupbox
            this.Margin = new Thickness( (this._category == Category.NORMAL ? 0 : 7), 0, (this._category == Category.NORMAL ? 0 : 10), (this._category == Category.NORMAL ? 4 : 6) );
            this.Style = (Style)App.Current.FindResource("groupBoxWithBorder");

            this.FontFamily = (FontFamily)App.Current.FindResource("Ubuntu");
            this.FontWeight = FontWeights.Bold;

            this.Tag = this._tip;

            this.BorderBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString( (this._category == Category.NORMAL ? "#1E222E" : "#5A5336") ));
            this.BorderThickness = new Thickness( (this._category == Category.NORMAL ? 0.4 : 0.7) );

            // Initializing content of groupbox
            StackPanel content = new StackPanel();

            // Grid of stackpanel
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            // Top left
            StackPanel topLeft = new StackPanel();
            topLeft.Orientation = Orientation.Horizontal;
            topLeft.Margin = new Thickness(0, 12, 0, 5);

            // Top left | icon
            Image icon = new Image();
            icon.Source = new BitmapImage(new Uri("../../Images/Donation.ico", UriKind.RelativeOrAbsolute));
            icon.Height = 20;
            icon.Width = 20;
            icon.Margin = new Thickness(3, 0, 0, 0); //check top
            icon.VerticalAlignment = VerticalAlignment.Center;

            // Top left | username
            TextBlock username = new TextBlock();
            username.Margin = new Thickness(5, 0, 0, 0); //check top
            username.Text = _tip.user.username;
            username.Foreground = new SolidColorBrush(Color.FromRgb(91, 187, 165));
            username.VerticalAlignment = VerticalAlignment.Center;

            // Top left | amount
            TextBlock amount_Outer = new TextBlock();
            amount_Outer.Margin = new Thickness(5, 0, 0, 0);

            Border amount_Border = new Border();
            amount_Border.BorderBrush = new SolidColorBrush(_category == Category.HIGHEST ? gold : gray);
            amount_Border.Background = new SolidColorBrush(_category == Category.HIGHEST ? gold : gray);
            amount_Border.CornerRadius = new CornerRadius(10);

            TextBlock amount_Inner = new TextBlock();
            amount_Inner.Text = formatCurrency(_tip.amount, _tip.currency);
            amount_Inner.Margin = new Thickness(10, 4, 10, 4);
            amount_Inner.Foreground = Brushes.FloralWhite;

            amount_Border.Child = amount_Inner;
            amount_Outer.Inlines.Add(amount_Border);

            // Add to top left
            topLeft.Children.Add(icon);
            topLeft.Children.Add(username);
            topLeft.Children.Add(amount_Outer);

            // Top right
            StackPanel topRight = new StackPanel();
            topRight.Orientation = Orientation.Horizontal;
            topRight.HorizontalAlignment = HorizontalAlignment.Right;
            topRight.VerticalAlignment = VerticalAlignment.Top;
            topRight.Margin = new Thickness(0, 12, 4, 0);

            // Top right | donation ... ago
            TextBlock timeAgo = new TextBlock();
            timeAgo.Margin = new Thickness(0, 0, 4, 0);
            timeAgo.VerticalAlignment = VerticalAlignment.Center;
            timeAgo.Text = timeDifference(_tip.activity.createdAt);
            timeAgo.Foreground = Brushes.Gray;

            this.timeAgo_TextBlock = timeAgo;

            // Top right | replay button
            Button replay = new Button();
            replay.Background = Brushes.Transparent;
            replay.BorderThickness = new Thickness(0);
            replay.VerticalAlignment = VerticalAlignment.Center;
            replay.Style = (Style)App.Current.FindResource("button");

            replay.PreviewMouseUp += (sender, e) =>
            {
                this.replay_Image.Source = new BitmapImage(new Uri("../../Images/Replay_Lightgray.ico", UriKind.RelativeOrAbsolute));
            };
            replay.PreviewMouseDown += (sender, e) =>
            {
                this.replay_Image.Source = new BitmapImage(new Uri("../../Images/Replay_Green.ico", UriKind.RelativeOrAbsolute));
            };

            // Top right | replay button | image
            Image replayIcon = new Image();
            replayIcon.Source = new BitmapImage(new Uri("../../Images/Replay_Lightgray.ico", UriKind.RelativeOrAbsolute));
            replayIcon.Height = 20;
            replayIcon.Width = 20;

            this.replay_Image = replayIcon;

            replay.Content = replayIcon;
            replay.Click += Replay_Click;

            // Add to top right
            topRight.Children.Add(timeAgo);
            topRight.Children.Add(replay);

            // Message panel
            WrapPanel? messagePanel = null;
            if (_tip.message.Length > 0)
            {
                messagePanel = new WrapPanel();
                messagePanel.Margin = new Thickness(3, 0, 3, 5);

                // Message panel | message
                RichTextBox message = new RichTextBox();
                message.IsReadOnly = true;
                message.IsDocumentEnabled = true;
                message.Background = new SolidColorBrush(Color.FromRgb(3, 7, 19));
                message.BorderThickness = new Thickness(0);

                Paragraph paragraph = new Paragraph();
                paragraph.Foreground = Brushes.FloralWhite;

                foreach (string segment in _tip.message.Split(' '))
                {
                    if (Uri.IsWellFormedUriString(segment, UriKind.Absolute))
                    {
                        Hyperlink hyperlink = new Hyperlink(new Run(segment));
                        hyperlink.NavigateUri = new Uri(segment);
                        hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(105, 60, 153));
                        hyperlink.RequestNavigate += (sender, e) =>
                        {
                            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                            Console.WriteLine($"Opened url {e.Uri.AbsoluteUri}");
                        };

                        paragraph.Inlines.Add(hyperlink);
                        paragraph.Inlines.Add(new Run(" "));
                    }
                    else paragraph.Inlines.Add(new Run(segment + " "));
                }

                FlowDocument flowDocument = new FlowDocument();
                flowDocument.Blocks.Add(paragraph);

                message.Document = flowDocument;
                messagePanel.Children.Add(message);
            }

            // Add to grid
            Grid.SetRow(topLeft, 0);
            Grid.SetRow(topRight, 0);
            if (messagePanel != null) Grid.SetRow(messagePanel, 1);

            grid.Children.Add(topLeft);
            grid.Children.Add(topRight);
            if (messagePanel != null) grid.Children.Add(messagePanel);

            // Add grid to stackpanel
            content.Children.Add(grid);

            this.Content = content;
        }

        private void Replay_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
                            {
                                using StringContent jsonContent = new StringContent("", Encoding.UTF8, "application/json");

                                using HttpResponseMessage responseMessage = await App.httpClient.PostAsync(string.Format(STREAMELEMENTS_REPLAY_API, App.se_service.channelId, _tip.activity.id), jsonContent);
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


        public enum Category
        {
            NORMAL,
            HIGHEST,
        }

    }
}
