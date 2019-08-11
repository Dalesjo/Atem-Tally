using AtemApi.Entities;
using AtemApi.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtemApi.Hubs
{

    public class TallyHub : Hub<ITallyClient>
    {

        /// <summary>
        /// Nlog.
        /// </summary>
        private readonly ILogger log;

        public TallyHub(ILogger<TallyHub> logger)
        {
            log = logger;
        }

        public override async Task OnConnectedAsync()
        {
            log.LogInformation($"Device connected with connection id {Context.ConnectionId}");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            log.LogInformation($"Device with connection id {Context.ConnectionId} disconnected");
        }

        public async Task SendTally(Tally tally)
        {
            await Clients.All.ReceiveTally(tally);
        }
    }
}
