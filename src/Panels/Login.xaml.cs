using System.Windows;

namespace Streamstats.src.Panels
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private bool CURRENTLY_LOGGING_IN = false;

        private Thickness NOTIFICATION_PANEL_THICKNESS = new Thickness(0, 15, 15, 15);

        public Login()
        {
            InitializeComponent();

            if (App.config.jwtToken != null && App.config.jwtToken.Length > 0) textBox_jwtToken.Text = App.config.jwtToken;
            if (App.config.twitchToken != null && App.config.twitchToken.Length > 0) textBox_twitchToken.Text = App.config.twitchToken;

            button_Login.Click += Button_Login;
        }

        private void Button_Login(object sender, RoutedEventArgs e)
        {
            if (CURRENTLY_LOGGING_IN)
            {
                notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Already logging in. Please wait.", NOTIFICATION_PANEL_THICKNESS));
                return;
            }

            if (textBox_jwtToken.Text.Length > 0
                && textBox_twitchToken.Text.Length > 0)
            {
                App.config.jwtToken = textBox_jwtToken.Text;
                App.config.twitchToken = textBox_twitchToken.Text;
                App.config.save();

                connect();

                CURRENTLY_LOGGING_IN = true;
            }
            else
            {
                notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Please enter your credentials", NOTIFICATION_PANEL_THICKNESS));
            }
        }

        private async Task connect()
        {
            int trys = 0;
            do
            {
                if (trys >= 5)
                {
                    CURRENTLY_LOGGING_IN = false;
                    notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", "Could not log in", NOTIFICATION_PANEL_THICKNESS));
                    Console.WriteLine($"Stopped connection after {trys} attemptions");
                    return;
                }

                trys++;
                Console.WriteLine("Trying to connect ...");
                notificationCenter.Children.Add(new src.Notification.Notification(7, "#C80815", "#860111", "#f5f5f5", $"Try to log in.. ({trys} / 5)", NOTIFICATION_PANEL_THICKNESS));

                await App.se_service.ConnectSocket();
                await Task.Delay(2000);

                if (App.se_service.CONNECTED) break;
            } while (!App.se_service.CONNECTED);

            Console.WriteLine($"{trys} attemption(s) were needed | Switching to panel");

            this.Hide();
            new src.Panels.Panel().Show();
        }
    }
}