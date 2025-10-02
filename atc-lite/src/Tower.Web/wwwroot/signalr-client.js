// Simple SignalR client for ATC-Lite Tower
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub/notify")
    .configureLogging(signalR.LogLevel.Information)
    .build();


// Expect the server to send the full state object for real-time updates
connection.on("stateUpdated", state => {
    if (window.signalrUpdate) {
        window.signalrUpdate(state);
    }
});

connection.on("advisoryRaised", adv => {
    // Show advisory in the alert badge
    const badge = document.getElementById('alertBadge');
    if (badge) {
        badge.textContent = typeof adv === 'string' ? adv : (adv?.message || JSON.stringify(adv));
        badge.style.display = '';
        setTimeout(() => { badge.style.display = 'none'; }, 5000);
    }
});

connection.start()
    .then(() => { window.signalrReady = true; })
    .catch(err => console.error(err));
