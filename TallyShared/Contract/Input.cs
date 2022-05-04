using LibAtem.Common;

namespace TallyServer.Contract
{
    public class Input
    {
        public string Id { get; set; }

        public string LongName { get; set; }

        public string ShortName { get; set; }

        public bool Program { get; set; }

        public bool Preview { get; set; }

        public List<Mixer> Mixer { get; set; } = new List<Mixer>();


        public void NewMixer(MixEffectBlockId mixerId)
        {
            var newMixer = new Mixer()
            {
                MixerId = mixerId,
                Program = false,
                Preview = false
            };
            
            Mixer.Add(newMixer);
        }
    }

    public class Mixer
    {
        public MixEffectBlockId MixerId { get; set; }

        public bool Program { get; set; }

        public bool Preview { get; set; }

    }
}


