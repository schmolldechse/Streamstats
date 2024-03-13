using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Streamstats.src.Panels
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private bool CURRENTLY_LOGGING_IN = false;

        private Thickness NOTIFICATION_PANEL_THICKNESS = new Thickness(0, 0, 15, 15);

        public Login()
        {
            InitializeComponent();

            if (App.config.jwtToken != null && App.config.jwtToken.Length > 0) textBox_jwtToken.Password = App.config.jwtToken;

            this.button_Login.Click += Button_Login;
            this.redirect_SE_Dashboard.NavigateUri = new Uri("https://streamelements.com/dashboard/account/channels");
            this.redirect_SE_Dashboard.RequestNavigate += (sender, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                Console.WriteLine($"Opened url {e.Uri.AbsoluteUri}");
            };
        }

        private void Button_Login(object sender, RoutedEventArgs e)
        {
            if (this.CURRENTLY_LOGGING_IN)
            {
                this.notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Already logging in. Please wait.", NOTIFICATION_PANEL_THICKNESS));
                return;
            }

            if (this.textBox_jwtToken.Password.Length <= 0)
            {
                this.notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Please enter your credentials", NOTIFICATION_PANEL_THICKNESS));
                return;
            }

            App.config.jwtToken = this.textBox_jwtToken.Password;
            App.config.save();

            this.connect();
        }

        private void Paste_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsText()) return;

            string clipboard = Clipboard.GetText();
            this.textBox_jwtToken.Password = clipboard;

            this.notificationCenter.Children.Add(new src.Notification.Notification(4, "#4CAF50", "#388E3C", "#f5f5f5", "Pasted", NOTIFICATION_PANEL_THICKNESS));
        }

        private async Task connect()
        {
            this.CURRENTLY_LOGGING_IN = true;

            int tries = 0;
            const int maxTries = 5;

            bool connected = false;

            while (!connected && tries < maxTries)
            {
                tries++;

                Console.WriteLine($"Trying to connect... Attempt {tries}/{maxTries}");
                this.notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", $"Trying to log in.. ({tries}/{maxTries})", NOTIFICATION_PANEL_THICKNESS));

                await App.se_service.ConnectSocket(
                               (success, data) =>
                               {
                                   connected = success;

                                   if (!connected)
                                   {
                                       Console.WriteLine("Connection attempt failed. Retrying...");
                                       App.Current.Dispatcher.InvokeAsync(() => this.notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", $"Attempt {tries} failed", NOTIFICATION_PANEL_THICKNESS)));
                                   }
                               });

                if (tries != maxTries) await Task.Delay(2000); // wait for next attempt
            }

            this.CURRENTLY_LOGGING_IN = false;

            if (!connected)
            {
                notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Could not log in", NOTIFICATION_PANEL_THICKNESS));
                Console.WriteLine($"Stopped connection after {tries} attemptions");
                return;
            }

            Console.WriteLine($"Connected successfully after {tries} attempt(s). Switching to panel");

            this.Hide();
            new Panel().Show();
        }
    }
}