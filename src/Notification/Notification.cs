using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Streamstats.src.Notification
{
    public class Notification : GroupBox
    {

        private DispatcherTimer timer;

        public Notification(int seconds, string backgroundColor, string borderColor, string messageColor, string message, Thickness thickness)
        {
            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(seconds);
            this.timer.Tick += Timer_Tick;
            this.timer.Start();

            this.Style = (Style) App.Current.FindResource("groupBoxWithBorder");

            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor));

            this.BorderBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString(borderColor));
            this.BorderThickness = new Thickness(0.7);

            this.Margin = thickness; //login 0 15 15 15 | panel 0 15 0 0

            this.HorizontalAlignment = HorizontalAlignment.Right;

            //STACKPANEL
            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(10, 8, 10, 5);
            stackPanel.Orientation = Orientation.Horizontal;

            //STACKPANEL | MESSAGE
            TextBlock messageBlock = new TextBlock();
            messageBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(messageColor));
            messageBlock.FontWeight = FontWeights.Bold;
            messageBlock.FontFamily = (FontFamily)App.Current.FindResource("Ubuntu");
            messageBlock.Text = message;
            messageBlock.VerticalAlignment = VerticalAlignment.Center;

            //STACKPANEL | DELETE BUTTON
            Button deleteButton = new Button();
            deleteButton.Content = "X";
            deleteButton.FontWeight = FontWeights.Bold;
            deleteButton.FontFamily = (FontFamily)App.Current.FindResource("Ubuntu");
            deleteButton.Margin = new Thickness(13, 0, 0, -2);
            deleteButton.Foreground = Brushes.Black;
            deleteButton.Background = Brushes.Transparent;
            deleteButton.BorderThickness = new Thickness(0);
            deleteButton.HorizontalAlignment = HorizontalAlignment.Right;

            deleteButton.Click += (sender, e) =>
            {
                (this.Parent as StackPanel)?.Children.Remove(this);
                if (this.timer.IsEnabled) this.timer.Stop();
            };

            stackPanel.Children.Add(messageBlock);
            stackPanel.Children.Add(deleteButton);

            this.Content = stackPanel;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            (this.Parent as StackPanel)?.Children.Remove(this);
            this.timer.Stop();
        }
    }
}
