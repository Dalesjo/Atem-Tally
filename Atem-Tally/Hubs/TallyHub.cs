using Microsoft.AspNetCore.SignalR;
using TallyServer.Contract;
using TallyShared.Contract;

namespace TallyServer.Hubs
{
    public class TallyHub : Hub<ITallyClient>
    {
        ILogger<TallyHub> Log;

        public TallyHub(
            ILogger<TallyHub> log)
        {
            Log = log;
        }

        public override async Task OnConnectedAsync()
        {
            Log.LogInformation($"Device connected with connection id {Context.ConnectionId}");
            //await Clients.Caller.ReceiveTally(tallyService.tally);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Log.LogInformation($"Device with connection id {Context.ConnectionId} disconnected");
        }

        public async Task RegisterTally(string tally)
        {
            await  Groups.AddToGroupAsync(Context.ConnectionId, tally);
            Log.LogInformation($"RegisterTally {Context.ConnectionId} as {tally}");

        }

        public async Task UnregisterTally(string tally)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tally);
            Log.LogInformation($"UnregisterTally {Context.ConnectionId} as {tally}");
            
        }
    }
}
