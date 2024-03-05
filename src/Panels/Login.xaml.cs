using Streamstats.src.Panels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Streamstats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Login : Window
    {

        public Login()
        {
            InitializeComponent();

            if (App.config.jwtToken != null && App.config.jwtToken.Length > 0) textBox_jwtToken.Text = App.config.jwtToken;
            if (App.config.twitchToken != null && App.config.twitchToken.Length > 0) textBox_twitchToken.Text = App.config.twitchToken;

            button_Login.Click += Button_Login;
        }

        private void Button_Login(object sender, RoutedEventArgs e)
        {
            if (!button_Login.IsEnabled) return;

            if (textBox_jwtToken.Text.Length > 0 
                && textBox_twitchToken.Text.Length > 0)
            {
                App.config.jwtToken = textBox_jwtToken.Text;
                App.config.twitchToken = textBox_twitchToken.Text;
                App.config.save();

                connect();

                button_Login.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Please enter your credentials");
            }
        }

        private async Task connect()
        {
            int trys = 0;
            do
            {
                if (trys >= 5)
                {
                    button_Login.IsEnabled = true;
                    Console.WriteLine($"Stopped connection after {trys} attemptions");
                    return;
                }

                Console.WriteLine("Trying to connect ...");
                trys++;

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