using System.Windows;
using System.Windows.Media.TextFormatting;

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

            button_Login.Click += Button_Login;
        }

        private void Button_Login(object sender, RoutedEventArgs e)
        {
            if (CURRENTLY_LOGGING_IN)
            {
                notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Already logging in. Please wait.", NOTIFICATION_PANEL_THICKNESS));
                return;
            }

            if (textBox_jwtToken.Password.Length <= 0)
            {
                notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Please enter your credentials", NOTIFICATION_PANEL_THICKNESS));
                return;
            }

            App.config.jwtToken = textBox_jwtToken.Password;
            App.config.save();

            connect();
        }

        private async Task connect()
        {
            CURRENTLY_LOGGING_IN = true;

            int tries = 0;
            const int maxTries = 5;

            bool connected = false;

            while (!connected && tries < maxTries)
            {
                tries++;

                Console.WriteLine($"Trying to connect... Attempt {tries}/{maxTries}");
                notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", $"Trying to log in.. ({tries}/{maxTries})", NOTIFICATION_PANEL_THICKNESS));

                await App.se_service.ConnectSocket(
                               (success, data) =>
                               {
                                   connected = success;

                                   if (!connected)
                                   {
                                       Console.WriteLine("Connection attempt failed. Retrying...");
                                       App.Current.Dispatcher.InvokeAsync(() => notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", $"Attempt {tries} failed", NOTIFICATION_PANEL_THICKNESS)));
                                   }
                               });

                if (tries != maxTries) await Task.Delay(2000); // wait for next attempt
            }

            CURRENTLY_LOGGING_IN = false;

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