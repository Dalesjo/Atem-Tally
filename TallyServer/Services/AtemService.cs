using LibAtem.Commands;
using LibAtem.Commands.CameraControl;
using LibAtem.Commands.MixEffects;
using LibAtem.Commands.Settings;
using LibAtem.Common;
using LibAtem.Net;
using Microsoft.AspNetCore.SignalR;
using TallyServer.Contract;
using TallyServer.Hubs;
using TallyShared.Contract;

namespace TallyServer.Services
{
    public class AtemService : IHostedService
    {
        public AtemService(
            AtemSettings atemSettings,
            AtemStatus atemStatus,
            ILogger<AtemService> logger,
            IHubContext<TallyHub, ITallyClient> tallyHub)
        {
            Log = logger;
            AtemSettings = atemSettings;
            TallyHub = tallyHub;
            AtemStatus = atemStatus;

            Client = new AtemClient(AtemSettings.Mixer);
            Client.OnReceive += OnCommand;
            Client.OnConnection += onConnect;
            Client.OnDisconnect += onDisconnect;
        }

        public IHubContext<TallyHub, ITallyClient> TallyHub { get; private set; }
        private AtemSettings AtemSettings { get; }

        private AtemStatus AtemStatus { get; set; }
        private AtemClient Client { get; set; }
        private bool firstCommand { get; set; } = true;
        private ILogger Log { get; set; }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.LogInformation($"Starting AtemService");
            Client.Connect();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.LogInformation($"Stopping AtemService");
            Client.Dispose();
            return Task.CompletedTask;
        }

        private bool IsMixerDisabled(MixEffectBlockId index, string source)
        {
            if (index == MixEffectBlockId.One && AtemSettings.ME1 == false)
            {
                return true;
            }

            if (index == MixEffectBlockId.Two && AtemSettings.ME1 == false)
            {
                return true;
            }

            if (index == MixEffectBlockId.Three && AtemSettings.ME1 == false)
            {
                return true;
            }

            if (index == MixEffectBlockId.Four && AtemSettings.ME1 == false)
            {
                return true;
            }

            return false;
        }

        private void OnCommand(object sender, IReadOnlyList<ICommand> commands)
        {
            try
            {
                var sendUpdate = false;

                foreach (ICommand cmd in commands)
                {
                    if (cmd is InputPropertiesGetCommand inputPropertiesGetCommand)
                    {
                        OnInput(inputPropertiesGetCommand);
                    };


                    if (cmd is TallyBySourceCommand tallyBySourceCommand)
                    {
                        OnTally(tallyBySourceCommand);
                        sendUpdate = true;
                    }

                    if (cmd is ProgramInputGetCommand programInputGetCommand)
                    {
                        OnProgram(programInputGetCommand);
                        sendUpdate = true;
                    }

                    if (cmd is PreviewInputGetCommand previewInputGetCommand)
                    {
                        OnPreview(previewInputGetCommand);
                        sendUpdate = true;
                    }
                }

                if (sendUpdate)
                {
                    Task.WhenAll(Update());
                }
            }
            catch (Exception e)
            {
                Log.LogError("Exception Inside onCommand", e);
            }
        }

        private void onConnect(object sender)
        {
            Log.LogInformation($"Connected to ATEM {AtemSettings.Mixer}");
        }

        /// <summary>
        /// Log disconnects from ATEM.
        /// </summary>
        /// <param name="sender"></param>
        private void onDisconnect(object sender)
        {
            Log.LogInformation($"Disconnected from ATEM ${AtemSettings.Mixer}");
        }
        private void OnInput(InputPropertiesGetCommand inputPropertiesGetCommand)
        {
            var input = new Input()
            {
                Id = inputPropertiesGetCommand.Id.ToString(),
                LongName = inputPropertiesGetCommand.LongName,
                ShortName = inputPropertiesGetCommand.ShortName,
                Preview = false,
                Program = false,
                Mixer = new List<Mixer>()
            };

            if(AtemSettings.ME1 && AtemSettings.ME1Inputs.Any(i => i == inputPropertiesGetCommand.Id.ToString()))
            {
                input.NewMixer(MixEffectBlockId.One);
            }

            if (AtemSettings.ME2 && AtemSettings.ME2Inputs.Any(i => i == inputPropertiesGetCommand.Id.ToString()))
            {
                input.NewMixer(MixEffectBlockId.Two);
            }

            if (AtemSettings.ME3 && AtemSettings.ME3Inputs.Any(i => i == inputPropertiesGetCommand.Id.ToString()))
            {
                input.NewMixer(MixEffectBlockId.Three);
            }

            if (AtemSettings.ME4 && AtemSettings.ME4Inputs.Any(i => i == inputPropertiesGetCommand.Id.ToString()))
            {
                input.NewMixer(MixEffectBlockId.Four);
            }

            Log.LogInformation($"Input  '{input.Id}' / '{input.LongName}'");

            AtemStatus.Inputs.Add(input);
        }

        /// <summary>
        /// Used to capture any mixer (ME1, ME2, ME3 ...) that is using an input but that is not pushed to the preview output.
        /// </summary>
        /// <param name="previewInputGetCommand"></param>
        private void OnPreview(PreviewInputGetCommand previewInputGetCommand)
        {
            var index = previewInputGetCommand.Index;
            var preview = previewInputGetCommand.Source.ToString();
            if (IsMixerDisabled(index, preview))
            {
                return;
            }
            
            if(!AtemSettings.MEPreview)
            {
                return;
            }

            var input = AtemStatus.Inputs.FirstOrDefault(c => c.Id == preview);
            if (input == null)
            {
                return;
            }

            /* Clear preview lamps */
            var otherLamps = AtemStatus.Inputs
                .SelectMany(i => i.Mixer)
                .Where(m => m.MixerId == index)
                .ToList();

            /* Set new lamp */
            var thisProgram = AtemStatus.Inputs
                .Where(i => i.Id == preview)
                .SelectMany(i => i.Mixer)
                .Where(m => m.MixerId == index)
                .ToList();

            if (!thisProgram.Any())
            {
                return;
            }
            
            otherLamps.ForEach(m => m.Preview = false);
            thisProgram.ForEach(m => m.Preview = true);

            Log.LogTrace($"onPreview {index} {preview}");
        }
        /// <summary>
        /// Used to capture any mixer (ME1, ME2, ME3 ...) that is using an input but that is not pushed to the program output.
        /// </summary>
        /// <param name="previewInputGetCommand"></param>
        private void OnProgram(ProgramInputGetCommand programInputGetCommand)
        {
            var index = programInputGetCommand.Index;
            var program = programInputGetCommand.Source.ToString();
            if (IsMixerDisabled(index, program))
            {
                return;
            }

            var input = AtemStatus.Inputs.FirstOrDefault(c => c.Id == program);
            if (input == null)
            {
                return;
            }

            /* Clear preview lamps */
            var otherLamps = AtemStatus.Inputs
                .SelectMany(i => i.Mixer)
                .Where(m => m.MixerId == index)
                .ToList();

            /* Set new lamp */
            var thisProgram = AtemStatus.Inputs
                .Where(i => i.Id == program)
                .SelectMany(i => i.Mixer)
                .Where(m => m.MixerId == index)
                .ToList();

            if (!thisProgram.Any())
            {
                return;
            }

            otherLamps.ForEach(m => m.Program = false);
            thisProgram.ForEach(m => m.Program = true);

            Log.LogInformation($"onProgram {index} {program}");
        }

        /// <summary>
        /// Capture any input that is active on program/preview
        /// </summary>
        /// <param name="tallyBySourceCommand"></param>
        private void OnTally(TallyBySourceCommand tallyBySourceCommand)
        {
            if(AtemSettings.Tally == false)
            {
                return;
            }

            foreach (var source in tallyBySourceCommand.Tally)
            {
                var input = AtemStatus.Inputs.FirstOrDefault(c => c.Id == source.Key.ToString());

                if(input == null)
                {
                    return;
                }

                input.Program = source.Value.Item1;
                input.Preview = source.Value.Item2;

                Log.LogTrace($"onTally {input.LongName},{input.Program},{input.Preview}");
            }
        }

        /// <summary>
        /// Sends update about tally to all connected clients.
        /// </summary>
        /// <returns></returns>
        private async Task Update()
        {
            foreach (var input in AtemStatus.Inputs)
            {
                //Log.LogDebug($"{input.Id}, Program: {input.Program}, Preview: {input.Preview}");

                await UpdateRegistratedDevices(input);
                await TallyHub.Clients.All.RecieveChannel(input);
            }
        }

        /// <summary>
        /// Send update to specific clients.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task UpdateRegistratedDevices(Input input)
        {
            var tallies = AtemSettings.Inputs
                .Where(tally => tally.Key == input.Id && String.IsNullOrEmpty(tally.Value) == false)
                .Select(tally => new Tally()
                {
                    Name = tally.Value,
                    Program = input.Program || input.Mixer.Any(m => m.Program == true),
                    Preview = input.Preview || input.Mixer.Any(m => m.Preview == true)
                })
                .ToList();

            foreach (var tally in tallies)
            {
                Log.LogDebug($"{tally.Name}, Program: {tally.Program}, Preview: {tally.Preview}");
                await TallyHub.Clients.Group(tally.Name).ReceiveTally(tally);
            }
        }
    }
}