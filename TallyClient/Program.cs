using NLog.Extensions.Hosting;
using TallyClient;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSystemd()
    .ConfigureServices(services =>
    {
        services.AddSingleton<Settings>();
        services.AddHostedService<Worker>();
    }).ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    })
    .UseNLog()
    .Build();



await host.RunAsync();
