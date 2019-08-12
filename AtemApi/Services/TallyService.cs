using AtemApi.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtemApi.Services
{
    public class TallyService
    {
        public Tally tally;

        public TallyService()
        {
            tally = new Tally()
            {
                me1 = new Mixer()
                {
                    r = "",
                    g = ""
                },
                me2 = new Mixer()
                {
                    r = "",
                    g = ""
                },
                inputs = new List<Input>()
            };
        }
    }
}
