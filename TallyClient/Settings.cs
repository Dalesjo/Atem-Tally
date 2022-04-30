using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TallyClient
{
    public class Settings
    {
        public Settings(IConfiguration configuration)
        {
            var server = configuration.GetSection("Server");
            Host = server.GetValue<string>("host");
            Port = server.GetValue<int>("port");

            var section = configuration.GetSection("Tally");
            var tallies = section.GetChildren();

            foreach(var tally in tallies)
            {
                var setting = new TallySetting(tally);
                Lights.Add(setting);
            }
        }

        public List<TallySetting> Lights { get; set; } = new List<TallySetting>();

        public string Host { get; set; }

        public int Port { get; set; }

    }
}
