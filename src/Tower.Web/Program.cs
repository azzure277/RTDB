
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Shared.Infrastructure;
using Contracts;
using Tower.Web;

// Expose config endpoint for safe values
// (must be after app is built)


var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
services.AddSingleton<ITrafficRepository, RedisTrafficRepository>();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddSignalR();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
// Map SignalR hub endpoint
app.MapHub<Tower.Web.TowerHub>("/hub/notify");

// Map new endpoints
app.MapState();
app.MapIngest();

app.Run();
