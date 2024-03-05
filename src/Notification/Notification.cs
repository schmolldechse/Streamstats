using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Streamstats.src.Notification
{
    public class Notification : GroupBox
    {

        private DispatcherTimer timer;

        public Notification(int seconds, string backgroundColor, string borderColor, string messageColor, string message)
        {
            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(seconds);
            this.timer.Tick += Timer_Tick;
            this.timer.Start();

            this.Style = (Style) App.Current.FindResource("groupBoxWithBorder");

            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor));

            this.BorderBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString(borderColor));
            this.BorderThickness = new Thickness(0.7);

            this.Margin = new Thickness(0, 15, 0, 0);

            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(10, 8, 10, 5);

            TextBlock textBlock = new TextBlock();
            textBlock.Text = message;
            textBlock.Foreground = new SolidColorBrush((Color) ColorConverter.ConvertFromString(messageColor));
            textBlock.FontWeight = FontWeights.Medium;
            textBlock.FontSize = 13;

            stackPanel.Children.Add(textBlock);
            this.Content = stackPanel;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            (this.Parent as StackPanel)?.Children.Remove(this);
            this.timer.Stop();
        }
    }
}
