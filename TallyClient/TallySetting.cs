using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TallyClient
{
    public class TallySetting
    {
        public TallySetting(IConfigurationSection section)
        {
            Name = section.GetValue<string>("Name");
            Program = section.GetValue<int>("Program");
            Preview = section.GetValue<int>("Preview");

            if("High" == section.GetValue<string>("Pin"))
            {
                Pin = PinValue.High;
            } 
            else
            {
                Pin = PinValue.Low;
            }
        }

        /// <summary>
        /// Name of the Tally
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public int Program { get; }

        /// <summary>
        /// Pin number to show Tally Preview
        /// </summary>
        public int Preview { get; }

        /// <summary>
        /// Value Pin should be set to when it is enabled
        /// </summary>
        public PinValue Pin { get;  }
    }
}

    



