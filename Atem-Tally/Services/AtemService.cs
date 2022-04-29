using LibAtem.Commands;
using LibAtem.Net;

namespace Atem_Tally.Services
{
    public class AtemService : BackgroundService
    {

        private readonly ILogger log;
        private AtemClient client;

        public AtemService(
            IConfiguration configuration,
            ILogger<AtemService> logger)
        {
            log = logger;

        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            client = new AtemClient("192.168.59.130");
            client.OnReceive += OnCommand;
            client.OnConnection += onConnect;
            client.OnDisconnect += onDisconnect;
            client.Connect();

            return Task.CompletedTask;
        }

        private void onDisconnect(object sender)
        {
            log.LogInformation("Disconnected from ATEM.");
        }

        private void onConnect(object sender)
        {
            log.LogInformation("Connected to ATEM.");
        }

        private void OnCommand(object sender, IReadOnlyList<ICommand> commands)
        {
            var sendUpdate = false;

            try
            {
                foreach (ICommand cmd in commands)
                {
                    if (cmd is TallyBySourceCommand tallyBySourceCommand)
                    {
                        onTally(tallyBySourceCommand);

                    } 
                    
                    if (cmd is TallyByInputCommand tallyByInputCommand)
                    {

                    }

                    if(cmd is TallyChannelConfigCommand tallyChannelConfigCommand)
                    {

                    }

                    if(cmd is TallyTlFcCommand tallyTlFcCommand)
                    {

                    }
                }
            }
            catch (Exception e)
            {
                log.LogError("Exception Inside onCommand", e);
            }
        }

        private void onTally(TallyBySourceCommand tallyBySourceCommand)
        {
            foreach (var source in tallyBySourceCommand.Tally)
            {
                var key = source.Key.ToString();
                var bool1 = source.Value.Item1;
                var bool2 = source.Value.Item2;

                log.LogInformation($"Tally {key},{bool1},{bool2}");
            }
        }
    }
}
