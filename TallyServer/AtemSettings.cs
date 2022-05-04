namespace TallyServer
{
    public class AtemSettings
    {
        public AtemSettings(
            IConfiguration configuration,
            ILogger<AtemSettings> log)
        {
            var atem = configuration.GetSection("Atem");

            Mixer = atem.GetValue<string>("Mixer");

            Tally = atem.GetValue<bool>("Tally");
            ME1 = atem.GetValue<bool>("ME1");
            ME2 = atem.GetValue<bool>("ME2");
            ME3 = atem.GetValue<bool>("ME3");
            ME4 = atem.GetValue<bool>("ME4");

            ME1Inputs = atem.GetSection("ME1Inputs").Get<List<string>>();
            ME2Inputs = atem.GetSection("ME2Inputs").Get<List<string>>();
            ME3Inputs = atem.GetSection("ME3Inputs").Get<List<string>>();
            ME4Inputs = atem.GetSection("ME4Inputs").Get<List<string>>();

            if(ME1Inputs == null)
            {
                ME1Inputs = new List<String>();
            }

            if (ME2Inputs == null)
            {
                ME2Inputs = new List<String>();
            }

            if (ME3Inputs == null)
            {
                ME3Inputs = new List<String>();
            }

            if (ME4Inputs == null)
            {
                ME4Inputs = new List<String>();
            }

            MEPreview = atem.GetValue<bool>("MEPreview");

            Inputs = atem
                .GetSection("Inputs")
                .GetChildren()
                .ToDictionary(x => x.Key, x => x.Value);

            LogSettings(log);
        }

        private void LogSettings(ILogger<AtemSettings> log)
        {
            log.LogInformation("--- AtemSettings ---");
            log.LogInformation($"Mixer: {Mixer}");
            log.LogInformation($"ME1: {ME1}");
            log.LogInformation($"ME2: {ME2}");
            log.LogInformation($"ME3: {ME3}");
            log.LogInformation($"ME4: {ME4}");
            log.LogInformation($"MEPreview: {MEPreview}");
            log.LogInformation($"Configured Inputs");

            foreach(var input in Inputs)
            {
                log.LogInformation($"Input: {input.Key}, Tally: {input.Value}");
            }

        }


        /// <summary>
        /// The IP-number of the Atem Mixer we shall connect to.
        /// </summary>
        public string Mixer { get; }
        
        /// <summary>
        /// if we should turn on tally for ME1
        /// </summary>
        public bool ME1 { get; }

        /// <summary>
        /// if we should turn on tally for ME1
        /// </summary>
        public bool ME2 { get; }

        /// <summary>
        /// if we should turn on tally for ME1
        /// </summary>
        public bool ME3 { get; }

        /// <summary>
        /// if we should turn on tally for ME1
        /// </summary>
        public bool ME4 { get; }

        /// <summary>
        /// List of inputs Tally is enabled for.
        /// </summary>
        public List<String> ME1Inputs { get; }

        /// <summary>
        /// List of inputs Tally is enabled for.
        /// </summary>
        public List<String> ME2Inputs { get; }

        /// <summary>
        /// List of inputs Tally is enabled for.
        /// </summary>
        public List<String> ME3Inputs { get; }

        /// <summary>
        /// List of inputs Tally is enabled for.
        /// </summary>
        public List<String> ME4Inputs { get; }

        /// <summary>
        /// Tally on preview for ME1,ME2,ME3,ME4
        /// </summary>
        public bool MEPreview { get; }


        public bool Tally { get; }

        /// <summary>
        /// Dictionary with replacement names for each input.
        /// </summary>
        public Dictionary<string, string> Inputs { get; }
    }





}
