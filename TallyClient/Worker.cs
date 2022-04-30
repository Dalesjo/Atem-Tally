using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using TallyServer.Contract;
using TallyShared.Contract;

namespace TallyClient
{
    public class Worker : BackgroundService
    {
        public Worker(ILogger<Worker> log)
        {
            Log = log;

            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:8000/tally")
                .WithAutomaticReconnect(new TallyRetryPolicy())
                .Build();

            connection.On<Tally>("ReceiveTally", OnTally);
            connection.On<Input>("RecieveInput", OnInput);

            connection.Closed += OnClosed;
            connection.Reconnected += OnReconnected;
            connection.Reconnecting += OnReconnecting;
        }

        private void OnInput(Input Input)
        {
            Log.LogInformation($"OnInput {Input.Id} {Input.Program} {Input.Preview}");

        }

        private HubConnection connection { get; set; }
        private ILogger<Worker> Log { get; set; }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ConnectWithRetryAsync(stoppingToken);
        }


        private async Task<bool> ConnectWithRetryAsync(CancellationToken stoppingToken)
        {
            // Keep trying to until we can start or the token is canceled.
            while (true)
            {
                try
                {
                    await connection.StartAsync(stoppingToken);
                    Debug.Assert(connection.State == HubConnectionState.Connected);
                    Log.LogInformation("Connection established with server");

                    await connection.SendAsync("RegisterTally", "Tally01");
                    Log.LogInformation("Register as Tally01");

                    return true;
                }
                catch when (stoppingToken.IsCancellationRequested)
                {
                    return false;
                }
                catch
                {
                    // Failed to connect, trying again in 5000 ms.
                    Log.LogError("Could not establish connection to server");
                    Debug.Assert(connection.State == HubConnectionState.Disconnected);
                    await Task.Delay(5000);
                }
            }
        }


        private async Task OnClosed(Exception? error)
        {
            var timeout = new Random().Next(0, 5) * 1000;

            Log.LogInformation($"Connection Lost, reconnection in {timeout} seconds");

            await Task.Delay(timeout);
            await connection.StartAsync();
        }

        private Task OnReconnected(string? arg)
        {
            Log.LogInformation($"Connection established");
            return Task.CompletedTask;
        }

        private Task OnReconnecting(Exception? arg)
        {
            Log.LogInformation($"Connection lost");
            return Task.CompletedTask;
        }
        private void OnTally(Tally tally)
        {
            Log.LogInformation($"OnTally {tally.Name} {tally.Program} {tally.Preview}");
        }
    }
}