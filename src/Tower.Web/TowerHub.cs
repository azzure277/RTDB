using Microsoft.AspNetCore.SignalR;

namespace Tower.Web
{
    public class TowerHub : Hub
    {
        // Broadcasts state updates to all clients
        public async Task StateUpdated(object state)
        {
            await Clients.All.SendAsync("stateUpdated", state);
        }

        // Broadcasts advisories to all clients
        public async Task AdvisoryRaised(object advisory)
        {
            await Clients.All.SendAsync("advisoryRaised", advisory);
        }
    }
}
