using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Shared.Infrastructure;
using Contracts;
using Tower.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights (optional, requires instrumentation key in config)
var aiConnStr = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConnStr))
	builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = aiConnStr);

// Auth config
string? tenantId = builder.Configuration["Auth:TenantId"];
string? audience = builder.Configuration["Auth:Audience"];
string authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority = authority;
		options.Audience = audience;
#if DEBUG
		options.RequireHttpsMetadata = false;
#endif
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidIssuer = authority,
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true
		};
	});

builder.Services.AddAuthorization(opts =>
{
	opts.AddPolicy("IngestWrite", policy =>
		policy.RequireAuthenticatedUser()
			  .RequireClaim("scp", "ingest.write")
			  .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme));
});

builder.Services.AddCors(o =>
	// Allow CORS for both production and local development
	o.AddPolicy("ui", p => p.WithOrigins("https://tower.example.com", "http://localhost:5000").AllowAnyHeader().AllowAnyMethod()));

// Prefer environment variable Redis__ConnectionString, fallback to config
var redisConnStr = Environment.GetEnvironmentVariable("Redis__ConnectionString") ?? builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redisConnStr))
    throw new Exception("Redis connection string not set. Set Redis__ConnectionString env var or Redis:ConnectionString in config.");
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnStr));
builder.Services.AddSingleton<ITrafficRepository, RedisTrafficRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var app = builder.Build();
app.UseCors("ui");
app.UseAuthentication();
app.UseAuthorization();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", ts = DateTime.UtcNow }))
	.WithName("Health");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
// Map SignalR hub endpoint
app.MapHub<Tower.Web.TowerHub>("/hub/notify");

// Map new endpoints
app.MapState();
// Protect /api/ingest with IngestWrite policy
app.MapMethods("/api/ingest", new[] { "POST" }, (HttpContext ctx) => Results.Accepted())
   .RequireAuthorization("IngestWrite");

app.Run();
