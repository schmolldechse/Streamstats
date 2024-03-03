using Streamstats.src.Config;
using Streamstats.src.Service.Streamelements;
using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Windows;

namespace Streamstats
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static Config config;

        public static HttpClient httpClient;

        public static StreamelementsService se_service;

        public App()
        {
            InitializeComponent();

            config = new Config();

            httpClient = new HttpClient();

            se_service = new StreamelementsService();
        }
    }

}
