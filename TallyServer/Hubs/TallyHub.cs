using Microsoft.AspNetCore.SignalR;
using TallyServer.Contract;
using TallyServer.Services;
using TallyShared.Contract;
#nullable disable

namespace TallyServer.Hubs
{
    public class TallyHub : Hub<ITallyClient>
    {
        readonly ILogger<TallyHub> Log;

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
            Log.LogInformation("Device connected with connection id {connectionId}", Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Log.LogInformation("Device with connection id {connectionId} disconnected", Context.ConnectionId);
        }

        public async Task RegisterTally(string tally)
        {
            await  Groups.AddToGroupAsync(Context.ConnectionId, tally);
            Log.LogInformation("RegisterTally {connectionId} as {tally}", Context.ConnectionId,tally);
            await Update();

        }

        public async Task UnregisterTally(string tally)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tally);
            Log.LogInformation("UnregisterTally {connectionId} as {tally}", Context.ConnectionId,tally);
            
        }

        /// <summary>
        /// Sends update about tally to all connected clients.
        /// </summary>
        /// <returns></returns>
        private async Task Update()
        {
            foreach (var input in AtemStatus.Inputs)
            {
                Log.LogDebug("{id}, Program: {program}, Preview: {preview}", input.Id,input.Program,input.Preview);

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
