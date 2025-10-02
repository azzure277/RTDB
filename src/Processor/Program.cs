using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Confluent.Kafka;
using Processor;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json", optional: true))
    .ConfigureServices((ctx, services) =>
    {
        // Redis connection
        var redisConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Redis") 
            ?? ctx.Configuration.GetConnectionString("Redis") 
            ?? "localhost:6379";
        
        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(redisConnectionString));
        
        // Kafka consumer
        var kafkaBootstrap = Environment.GetEnvironmentVariable("KAFKA") ?? "localhost:9092";
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrap,
            GroupId = "atc-processor",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };
        
        services.AddSingleton<IConsumer<string, string>>(sp =>
            new ConsumerBuilder<string, string>(consumerConfig).Build());
        
        services.AddSingleton<Shared.Infrastructure.ITrafficRepository, Shared.Infrastructure.RedisTrafficRepository>();
        services.AddHostedService<KafkaConsumerWorker>();
        services.AddHostedService<Worker>();
        services.AddHostedService<Processor.SequenceWorker>();
    })
    .ConfigureLogging(l => l.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }))
    .RunConsoleAsync();
