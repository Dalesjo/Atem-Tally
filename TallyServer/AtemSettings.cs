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
            
            ME1 = atem.GetValue<bool>("ME1");
            ME2 = atem.GetValue<bool>("ME2");
            ME3 = atem.GetValue<bool>("ME3");
            ME4 = atem.GetValue<bool>("ME4");
            Preview = atem.GetValue<bool>("Preview");



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
            log.LogInformation($"Preview: {Preview}");
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
        /// if we should turn on tally if ME1 is using camera.
        /// </summary>
        public bool ME1 { get; }

        /// <summary>
        /// if we should turn on tally if ME2 is using camera.
        /// </summary>
        public bool ME2 { get; }

        /// <summary>
        /// if we should turn on tally if ME3 is using camera.
        /// </summary>
        public bool ME3 { get; }

        /// <summary>
        /// if we should turn on tally if ME4 is using camera.
        /// </summary>
        public bool ME4 { get; }

        /// <summary>
        /// Tally on preview for ME1,ME2,ME3,ME4
        /// </summary>
        public bool Preview { get; }

        /// <summary>
        /// Dictionary with replacement names for each input.
        /// </summary>
        public Dictionary<string, string> Inputs { get; }
    }





}
