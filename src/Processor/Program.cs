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
    services.AddSingleton<Shared.Infrastructure.ITrafficRepository, Shared.Infrastructure.RedisTrafficRepository>();
    services.AddHostedService<Worker>();
    services.AddHostedService<Processor.SequenceWorker>();
    })
    .ConfigureLogging(l => l.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }))
    .RunConsoleAsync();
