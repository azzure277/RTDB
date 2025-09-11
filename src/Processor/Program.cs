using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json", optional: true))
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(ctx.Configuration["Redis:ConnectionString"]!));
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(l => l.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }))
    .RunConsoleAsync();
