using AtemApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LibAtem.Commands;
using LibAtem.Commands.Audio;
using LibAtem.Commands.DeviceProfile;
using LibAtem.Commands.MixEffects;
using LibAtem.Commands.MixEffects.Key;
using LibAtem.Commands.Settings;
using LibAtem.Net;
using Microsoft.Extensions.Configuration;
using AtemApi.Entities;

namespace AtemApi.Services
{
    public class AtemService : IHostedService, IDisposable
    {
        private Timer _timer;

        private AtemClient client;

        /// <summary>
        /// Nlog.
        /// </summary>
        private readonly ILogger log;

        /// <summary>
        /// Access to the signalR Hub.
        /// </summary>
        private IHubContext<TallyHub, ITallyClient> tallyhub;

        private TallyService tallyService;

        /// <summary>
        /// IP-number of ATEM mixer
        /// </summary>
        private string mixer;

        public bool ME2Tally { get; }


        /// <summary>
        /// Dictionary with replacement names for each input.
        /// </summary>
        private Dictionary<string, string> inputTallyNames;

        public AtemService(IConfiguration configuration, TallyService _tallyService, ILogger<AtemService> logger, IHubContext<TallyHub, ITallyClient> _tallyHub)
        {
            log = logger;
            tallyhub = _tallyHub;
            tallyService = _tallyService;

            mixer = configuration.GetSection("Settings:Atem").GetValue<string>("Mixer");

            ME2Tally = configuration.GetSection("Settings:Atem").GetValue<bool>("ME2Tally");

            inputTallyNames = configuration
                .GetSection("Settings:Atem:Inputs")
                .GetChildren()
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            client = new AtemClient(mixer);

            client.OnReceive += OnCommand;
            client.OnConnection += onConnect;
            client.OnDisconnect += onDisconnect;
            client.Connect();

            return Task.CompletedTask;
        }

        private string getTallyName(string input)
        {
            return inputTallyNames.GetValueOrDefault(input, input);
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
                    if (cmd is TallyBySourceCommand)
                    {
                        onTally(cmd as TallyBySourceCommand);
                        sendUpdate = true;
                    }
                    else if(ME2Tally && cmd is ProgramInputGetCommand)
                    {
                        onProgram(cmd as ProgramInputGetCommand);
                        sendUpdate = true;
                    }
                    else if (ME2Tally && cmd is PreviewInputGetCommand)
                    {
                        onPreview(cmd as PreviewInputGetCommand);
                        sendUpdate = true;
                    }
                }

                if(sendUpdate)
                {
                    tallyhub.Clients.All.ReceiveTally(tallyService.tally);
                }
            }
            catch (Exception e)
            {
                log.LogError("Exception Inside onCommand", e);
            }
        }

        private void onTally(TallyBySourceCommand tallyBySourceCommand)
        {
            log.LogInformation($"Tally has updated inputlist to {tallyBySourceCommand.Tally.Count} items.");
            var inputs = new List<Input>();
            foreach(var source in tallyBySourceCommand.Tally)
            {
                var input = new Input()
                {
                    n = getTallyName(source.Key.ToString()),
                    r = source.Value.Item1,
                    g = source.Value.Item2
                };

                inputs.Add(input);
            }

            tallyService.tally.inputs = inputs;
        }

        private void onPreview(PreviewInputGetCommand previewInputGetCommand)
        {
            var preview = getTallyName(previewInputGetCommand.Source.ToString());
            if (previewInputGetCommand.Index.ToString() == "Two")
            {
                // TWO
                log.LogInformation($"Preview changed for ME2 to {preview}");
                tallyService.tally.me2.g = preview;
            }
            else
            {
                // ONE
                log.LogInformation($"Preview changed for ME1 to {preview}");
                tallyService.tally.me1.g = preview;
            }
        }

        private void onProgram(ProgramInputGetCommand programInputGetCommand)
        {
            var program = getTallyName(programInputGetCommand.Source.ToString());
            if(programInputGetCommand.Index.ToString() == "Two")
            {
                // TWO
                log.LogInformation($"Preview changed for ME2 to {program}");
                tallyService.tally.me2.r = program;
            }
            else
            {
                // ONE
                log.LogInformation($"Preview changed for ME1 to {program}");
                tallyService.tally.me1.r = program;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
