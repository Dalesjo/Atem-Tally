﻿using LibAtem.Commands;
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
            Client.OnConnection += OnConnect;
            Client.OnDisconnect += OnDisconnect;
        }

        public IHubContext<TallyHub, ITallyClient> TallyHub { get; private set; }
        private AtemSettings AtemSettings { get; }

        private AtemStatus AtemStatus { get; set; }
        private AtemClient Client { get; set; }
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

        private bool IsMixerDisabled(MixEffectBlockId index)
        {
            if (index == MixEffectBlockId.One && AtemSettings.ME1 == false)
            {
                return true;
            }

            if (index == MixEffectBlockId.Two && AtemSettings.ME2 == false)
            {
                return true;
            }

            if (index == MixEffectBlockId.Three && AtemSettings.ME3 == false)
            {
                return true;
            }

            if (index == MixEffectBlockId.Four && AtemSettings.ME4 == false)
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
                        if(OnProgram(programInputGetCommand))
                        {
                            sendUpdate = true;
                        }
                    }

                    if (cmd is PreviewInputGetCommand previewInputGetCommand)
                    {
                        if(OnPreview(previewInputGetCommand))
                        {
                            sendUpdate = true;
                        }
                    }
                }

                if (sendUpdate)
                {
                    Update().Wait();
                }
            }
            catch (Exception e)
            {
                Log.LogError("Exception Inside onCommand", e);
            }
        }

        private void OnConnect(object sender)
        {
            Log.LogInformation("Connected to ATEM {mixer}",AtemSettings.Mixer);
        }

        /// <summary>
        /// Log disconnects from ATEM.
        /// </summary>
        /// <param name="sender"></param>
        private void OnDisconnect(object sender)
        {
            Log.LogInformation("Disconnected from ATEM {mixer}", AtemSettings.Mixer);
        }
        private void OnInput(InputPropertiesGetCommand inputPropertiesGetCommand)
        {
            if(AtemStatus.Inputs.Any(I => I.Id == inputPropertiesGetCommand.Id.ToString()))
            {
                Log.LogInformation("Input already registrated '{input}'",inputPropertiesGetCommand.Id);
                return;
            }

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

            Log.LogInformation("Registrated Input '{input}' / '{name}'", input.Id,input.LongName);

            AtemStatus.Inputs.Add(input);
        }

        /// <summary>
        /// Used to capture any mixer (ME1, ME2, ME3 ...) that is using an input but that is not pushed to the preview output.
        /// </summary>
        /// <param name="previewInputGetCommand"></param>
        private bool OnPreview(PreviewInputGetCommand previewInputGetCommand)
        {
            var index = previewInputGetCommand.Index;
            var preview = previewInputGetCommand.Source.ToString();
            if (IsMixerDisabled(index))
            {
                return false;
            }
            
            if(!AtemSettings.MEPreview)
            {
                return false;
            }

            var input = AtemStatus.Inputs.FirstOrDefault(c => c.Id == preview);
            if (input == null)
            {
                return false;
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
                return false;
            }
            
            otherLamps.ForEach(m => m.Preview = false);
            thisProgram.ForEach(m => m.Preview = true);

            Log.LogTrace("OnPreview {index} {preview}",index,preview);

            return true;
        }
        /// <summary>
        /// Used to capture any mixer (ME1, ME2, ME3 ...) that is using an input but that is not pushed to the program output.
        /// </summary>
        /// <param name="previewInputGetCommand"></param>
        private bool OnProgram(ProgramInputGetCommand programInputGetCommand)
        {
            var index = programInputGetCommand.Index;
            var program = programInputGetCommand.Source.ToString();
            if (IsMixerDisabled(index))
            {
                return false;
            }

            var input = AtemStatus.Inputs.FirstOrDefault(c => c.Id == program);
            if (input == null)
            {
                return false;
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
                return false;
            }

            otherLamps.ForEach(m => m.Program = false);
            thisProgram.ForEach(m => m.Program = true);

            Log.LogInformation("OnProgram {index} {program}",index,program);

            return true;
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

                Log.LogTrace("OnTally {name},{program},{preview}",input.LongName,input.Program,input.Preview);
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
                Log.LogDebug("{name}, Program: {program}, Preview: {preview}",tally.Name,tally.Program,tally.Preview);
                await TallyHub.Clients.Group(tally.Name).ReceiveTally(tally);
            }
        }
    }
}