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

        /// <summary>
        /// Current state of the tally
        /// </summary>
        private Tally tally;

        private AtemClient client;

        /// <summary>
        /// Nlog.
        /// </summary>
        private readonly ILogger log;

        /// <summary>
        /// Access to the signalR Hub.
        /// </summary>
        private IHubContext<TallyHub, ITallyClient> hub;

        /// <summary>
        /// IP-number of ATEM mixer
        /// </summary>
        private string mixer;

        /// <summary>
        /// Dictionary with replacement names for each input.
        /// </summary>
        private Dictionary<string, string> inputTallyNames;

        public AtemService(IConfiguration configuration, ILogger<AtemService> logger, IHubContext<TallyHub, ITallyClient> tallyHub)
        {
            log = logger;
            hub = tallyHub;

            mixer = configuration.GetSection("Settings:Atem").GetValue<string>("Mixer");

            inputTallyNames = configuration
                .GetSection("Settings:Atem:Inputs")
                .GetChildren()
                .ToDictionary(x => x.Key, x => x.Value);

            tally = new Tally()
            {
                me1 = new Mixer()
                {
                    program = "unknown",
                    preview = "unknown"
                },
                me2 = new Mixer()
                {
                    program = "unknown",
                    preview = "unknown"
                },
                inputs = new List<Input>()
            };
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
                    else if(cmd is ProgramInputGetCommand)
                    {
                        onProgram(cmd as ProgramInputGetCommand);
                        sendUpdate = true;
                    }
                    else if (cmd is PreviewInputGetCommand)
                    {
                        onPreview(cmd as PreviewInputGetCommand);
                        sendUpdate = true;
                    }
                }

                if(sendUpdate)
                {
                    hub.Clients.All.ReceiveTally(tally);
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
            foreach(var tally in tallyBySourceCommand.Tally)
            {
                var input = new Input()
                {
                    name = getTallyName(tally.Key.ToString()),
                    program = tally.Value.Item1,
                    preview = tally.Value.Item2
                };

                inputs.Add(input);
            }

            tally.inputs = inputs;
        }

        private void onPreview(PreviewInputGetCommand previewInputGetCommand)
        {
            var preview = getTallyName(previewInputGetCommand.Source.ToString());
            if (previewInputGetCommand.Index.ToString() == "Two")
            {
                // TWO
                log.LogInformation($"Preview changed for ME2 to {preview}");
                tally.me2.preview = preview;
            }
            else
            {
                // ONE
                log.LogInformation($"Preview changed for ME1 to {preview}");
                tally.me1.preview = preview;
            }
        }

        private void onProgram(ProgramInputGetCommand programInputGetCommand)
        {
            var program = getTallyName(programInputGetCommand.Source.ToString());
            if(programInputGetCommand.Index.ToString() == "Two")
            {
                // TWO
                log.LogInformation($"Preview changed for ME2 to {program}");
                tally.me2.program = program;
            }
            else
            {
                // ONE
                log.LogInformation($"Preview changed for ME1 to {program}");
                tally.me1.program = program;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
