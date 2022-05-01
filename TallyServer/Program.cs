using Microsoft.Extensions.Logging.EventLog;
using NLog.Web;
using TallyServer;
using TallyServer.Hubs;
using TallyServer.Services;

var webApplicationOptions = new WebApplicationOptions() 
{ 
    ContentRootPath = AppContext.BaseDirectory, 
    Args = args, 
    ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName 
}; 

var builder = WebApplication.CreateBuilder(webApplicationOptions); 
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<AtemSettings>();
builder.Services.AddSingleton<AtemStatus>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<AtemService>();

// NLog: Setup NLog for Dependency injection
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Host.UseNLog();

// Enable windows service
if (OperatingSystem.IsWindows())
{
    builder.Services.Configure<EventLogSettings>(config =>
    {
        config.LogName = "TallyServer";
        config.SourceName = "TallyServer";
    });

}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
}

app.UseStaticFiles();
app.UseRouting();
app.MapHub<TallyHub>("/tally");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html"); ;

app.Run();