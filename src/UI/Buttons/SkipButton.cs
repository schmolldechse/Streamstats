using System.DirectoryServices.ActiveDirectory;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Streamstats.src.UI.Buttons
{
    public class SkipButton : Button
    {

        /**
         * {0} represents the channelId
         */
        private readonly string API_URL = "https://api.streamelements.com/kappa/v3/overlays/{0}/action";

        private Image display;

        public SkipButton()
        {
            this.Background = Brushes.Transparent;
            this.BorderThickness = new Thickness(0);
            this.Margin = new Thickness(12, 0, 12, 0);

            this.display = new Image();
            this.display.Source = new BitmapImage(new Uri("../../../Images/Skip.ico", UriKind.RelativeOrAbsolute));
            this.display.Height = 25;
            this.display.Width = 25;

            this.Content = this.display;

            this.Style = (Style)App.Current.FindResource("button");
        }

        protected override async void OnClick()
        {
            base.OnClick(); 
            await HttpRequest();
        }

        private async Task HttpRequest()
        {
            StringContent jsonRequest = new StringContent(
                $"{{ \"action\" : \"skip\" }}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage responseMessage = await App.httpClient.PutAsync(string.Format(this.API_URL, App.se_service.channelId), jsonRequest);
            var jsonResponse = await responseMessage.Content.ReadAsStringAsync();

            Console.WriteLine($"Sent HTTP request : Action 'skip' and got {jsonResponse}");
        }
    }
}
