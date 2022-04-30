using Microsoft.AspNetCore.SignalR.Client;
using System.Device.Gpio;
using System.Diagnostics;
using TallyServer.Contract;
using TallyShared.Contract;

namespace TallyClient
{
    public class Worker : BackgroundService
    {
        public Worker(ILogger<Worker> log, Settings settings)
        {
            Log = log;
            Settings = settings;
            Controller = new GpioController();

            foreach (var light in Settings.Lights)
            {
                Controller.OpenPin(light.Program, PinMode.Output);
                Controller.OpenPin(light.Preview, PinMode.Output);

                Log.LogInformation($"Setup '{light.Name}', using pin Program: {light.Program}, Preview: {light.Preview}, On: {light.Pin}");
            }


            Connection = new HubConnectionBuilder()
                .WithUrl($"http://{Settings.Host}:{Settings.Port}/tally")
                .WithAutomaticReconnect(new TallyRetryPolicy())
                .Build();

            Connection.On("ReceiveTally", (Action<TallyShared.Contract.Tally>)this.OnTally);

            Connection.Closed += OnClosed;
            Connection.Reconnected += OnReconnected;
            Connection.Reconnecting += OnReconnecting;
        }

        private Settings Settings { get; set; }


        private bool IsBlinking { get; set;  }
        private Thread? BlinkingThread { get; set; }
        private HubConnection Connection { get; set; }
        private GpioController Controller { get; set; }
        private ILogger<Worker> Log { get; set; }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            startBlinking();
            await ConnectWithRetryAsync(stoppingToken);
            TurnOff();
        }

        private void TurnOff()
        {
            // Turn off Program/Preview, wait for server to give tally information.
            foreach (var light in Settings.Lights)
            {
                Controller.Write(light.Preview, light.Pin == PinValue.High ? PinValue.Low : PinValue.High);
                Controller.Write(light.Program, light.Pin == PinValue.High ? PinValue.Low : PinValue.High);
            }
        }

            private async Task<bool> ConnectWithRetryAsync(CancellationToken stoppingToken)
        {
            // Keep trying to until we can start or the token is canceled.
            while (true)
            {
                try
                {
                    await Connection.StartAsync(stoppingToken);
                    stopBlinking();
                    Log.LogInformation($"Connection established with server '{Settings.Host}:{Settings.Port}'");

                    // TODO
                    foreach (var light in Settings.Lights)
                    {
                        await Connection.SendAsync("RegisterTally", light.Name);
                        Log.LogInformation($"Register as '{light.Name}'");
                    }


                    return true;
                }
                catch when (stoppingToken.IsCancellationRequested)
                {
                    return false;
                }
                catch
                {
                    // Failed to connect, trying again in 5000 ms.
                    Log.LogWarning($"Could not establish connection to server '{Settings.Host}:{Settings.Port}'");
                    startBlinking();
                    await Task.Delay(5000);
                }
            }
        }


        private void startBlinking()
        {
            if (IsBlinking == true)
            {
                return;
            }

            IsBlinking = true;
            if (BlinkingThread == null)
            {
                BlinkingThread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Blinking();
                });
                BlinkingThread.Start();
            }
        }

        private void stopBlinking()
        {
            IsBlinking = false;

            if(BlinkingThread != null)
            {
                BlinkingThread.Join();
                BlinkingThread = null;
            }
            
        }

        private void Blinking()
        {
            // Turn off program
            foreach (var light in Settings.Lights)
            {
                Controller.Write(light.Program, light.Pin == PinValue.High ? PinValue.Low : PinValue.High);
            }

            var high = true;
            while (IsBlinking)
            {
                Log.LogInformation("Blinking");
                foreach (var light in Settings.Lights)
                {
                    Controller.Write(light.Preview, high ? PinValue.High : PinValue.Low);
                }

                high = !high;
                Thread.Sleep(500);
            }

            // Turn off Program/Preview, wait for server to give tally information.
            foreach (var light in Settings.Lights)
            {
                Controller.Write(light.Preview, light.Pin == PinValue.High ? PinValue.Low : PinValue.High);
                Controller.Write(light.Program, light.Pin == PinValue.High ? PinValue.Low : PinValue.High);
            }
        }

        private async Task OnClosed(Exception? error)
        {
            var timeout = new Random().Next(0, 5) * 1000;
            startBlinking();
            Log.LogWarning($"Connection lost to server '{Settings.Host}:{Settings.Port}', reconnection in {timeout} seconds");

            await Task.Delay(timeout);
            await Connection.StartAsync();
        }

        private Task OnReconnected(string? arg)
        {
            Log.LogInformation($"Connection restablished to server '{Settings.Host}:{Settings.Port}'");
            stopBlinking();
            return Task.CompletedTask;
        }

        private Task OnReconnecting(Exception? arg)
        {
            Log.LogWarning($"Connection lost to server '{Settings.Host}:{Settings.Port}'");
            startBlinking();
            return Task.CompletedTask;
        }
        private void OnTally(TallyShared.Contract.Tally tally)
        {
            Log.LogInformation($"OnTally {tally.Name} {tally.Program} {tally.Preview}");

            var light = Settings.Lights.FirstOrDefault(t => t.Name == tally.Name);

            if(light == null)
            {
                return;
            }

            if(light.Pin == PinValue.High)
            {
                Controller.Write(light.Program, tally.Program ? PinValue.High : PinValue.Low);
                Controller.Write(light.Preview, tally.Preview ? PinValue.High : PinValue.Low);
                return;
            }

            Controller.Write(light.Program, tally.Program ? PinValue.Low : PinValue.High);
            Controller.Write(light.Preview, tally.Preview ? PinValue.Low : PinValue.High);

        }
    }
}