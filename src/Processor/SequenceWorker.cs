using Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Processor
{
    public sealed class SequenceWorker(Shared.Infrastructure.ITrafficRepository repo, ILogger<SequenceWorker> log)
        : BackgroundService
    {
        const string Airport = "KSFO"; // make configurable
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var state = await repo.GetStateAsync(Airport, stoppingToken);
                    if (state.Positions.Count > 0)
                    {
                        var ordered = state.Positions
                            .OrderBy(p => p.AltFt) // placeholder: “lower first” as proxy for closer/landing
                            .ThenBy(p => p.TsUtc)
                            .Select(p => p.Id)
                            .ToList();

                        await repo.SetSequenceAsync(Airport, ordered, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Sequence worker error");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
