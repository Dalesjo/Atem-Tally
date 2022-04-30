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
            var section = configuration.GetSection("Tally");
            var tallies = section.GetChildren();

            foreach(var tally in tallies)
            {
                var setting = new TallySetting(tally);
                Lights.Add(setting);
            }
        }

        public List<TallySetting> Lights { get; set; } = new List<TallySetting>();

    }
}
