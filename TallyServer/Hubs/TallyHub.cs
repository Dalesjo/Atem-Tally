using Microsoft.AspNetCore.SignalR;
using TallyServer.Contract;
using TallyServer.Services;
using TallyShared.Contract;

namespace TallyServer.Hubs
{
    public class TallyHub : Hub<ITallyClient>
    {
        ILogger<TallyHub> Log;

        AtemSettings AtemSettings { get; set; }
        AtemStatus AtemStatus { get; set; }

        public TallyHub(
            ILogger<TallyHub> log,
            AtemSettings atemSettings,
            AtemStatus atemStatus)
        {
            Log = log;
            AtemSettings = atemSettings;
            AtemStatus = atemStatus;
        }

        public override async Task OnConnectedAsync()
        {
            Log.LogInformation($"Device connected with connection id {Context.ConnectionId}");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Log.LogInformation($"Device with connection id {Context.ConnectionId} disconnected");
        }

        public async Task RegisterTally(string tally)
        {
            await  Groups.AddToGroupAsync(Context.ConnectionId, tally);
            Log.LogInformation($"RegisterTally {Context.ConnectionId} as {tally}");
            await Update();

        }

        public async Task UnregisterTally(string tally)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tally);
            Log.LogInformation($"UnregisterTally {Context.ConnectionId} as {tally}");
            
        }

        /// <summary>
        /// Sends update about tally to all connected clients.
        /// </summary>
        /// <returns></returns>
        private async Task Update()
        {
            foreach (var input in AtemStatus.Inputs)
            {
                Log.LogDebug($"{input.Id}, Program: {input.Program}, Preview: {input.Preview}");

                await UpdateRegistratedDevices(input);
                await Clients.All.RecieveChannel(input);
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
                    Program = input.Program,
                    Preview = input.Preview
                })
                .ToList();

            foreach (var tally in tallies)
            {
                await Clients.Group(tally.Name).ReceiveTally(tally);
            }
        }
    }
}
