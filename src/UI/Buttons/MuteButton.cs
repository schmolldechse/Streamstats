using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Streamstats.src.UI.Buttons
{
    public class MuteButton : Button
    {

        /**
         * {0} represents the channelId
         */
        private readonly string API_URL = "https://api.streamelements.com/kappa/v3/overlays/{0}/action";

        private Image display;

        private bool ALERTS_MUTED = false;

        public MuteButton()
        {
            this.Background = Brushes.Transparent;
            this.BorderThickness = new Thickness(0);
            this.Margin = new Thickness(12, 0, 12, 0);

            this.display = new Image();
            this.display.Source = new BitmapImage(new Uri("../../../Images/Unmuted.ico", UriKind.RelativeOrAbsolute));
            this.display.Height = 20;
            this.display.Width = 20;

            this.Content = this.display;
            this.Tag = this.ALERTS_MUTED;

            this.Style = (Style)App.Current.FindResource("button");
        }

        protected override async void OnClick()
        {
            base.OnClick(); 
            
            this.ALERTS_MUTED = !this.ALERTS_MUTED;
            switch (this.ALERTS_MUTED)
            {
                case true:
                    this.display.Source = new BitmapImage(new Uri("../../../Images/Muted.ico", UriKind.RelativeOrAbsolute));
                    break;

                case false:
                    this.display.Source = new BitmapImage(new Uri("../../../Images/Unmuted.ico", UriKind.RelativeOrAbsolute));
                    break;
            }

            this.Content = this.display;
            await HttpRequest();
        }

        private async Task HttpRequest()
        {
            string action = this.ALERTS_MUTED ? "mute" : "unmute";
            StringContent jsonRequest = new StringContent(
                $"{{ \"action\" : \"{action}\" }}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage responseMessage = await App.httpClient.PutAsync(string.Format(this.API_URL, App.se_service.channelId), jsonRequest);
            var jsonResponse = await responseMessage.Content.ReadAsStringAsync();

            Console.WriteLine($"Sent HTTP request : Action '{action}' and got {jsonResponse}");
        }
    }
}
