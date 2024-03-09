using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streamstats.src.Config
{
    public class Config
    {
        public string jwtToken { get; set; }

        private readonly string path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "config.json");

        public Config()
        {
            init();
        }

        private void init()
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
                Console.WriteLine("Created config due to be non-existent");
            }

            string json = File.ReadAllText(path);
            JsonConvert.PopulateObject(json, this);

            Console.WriteLine("Read config: " + json);
        }

        public void save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(path, json);

                Console.WriteLine("Saved config");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Could not save config due to an error: " + exception.Message);
            }
        }
    }
}
