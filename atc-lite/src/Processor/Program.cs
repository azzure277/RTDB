using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json", optional: true))
    .ConfigureServices((ctx, services) =>
    {
        var redisConnStr = ctx.Configuration["Redis:ConnectionString"];
        Console.WriteLine($"[DEBUG] Redis:ConnectionString = '{redisConnStr}'");
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnStr!));
        services.AddSingleton<Shared.Infrastructure.ITrafficRepository, Shared.Infrastructure.RedisTrafficRepository>();
        services.AddHostedService<Worker>();
        services.AddHostedService<Processor.SequenceWorker>();
        // Add Application Insights if configured
        var aiConnStr = ctx.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(aiConnStr))
            services.AddApplicationInsightsTelemetryWorkerService(o => o.ConnectionString = aiConnStr);
    })
    .ConfigureLogging(l => l.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }))
    .RunConsoleAsync();
