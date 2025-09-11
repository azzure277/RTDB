// Simple SignalR client for ATC-Lite Tower
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub/notify")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("stateUpdated", seq => {
    // Update the landing sequence instantly
    const seqElem = document.getElementById('seq');
    seqElem.innerHTML = '';
    (seq || []).forEach(s => {
        const li = document.createElement('li');
        li.textContent = s;
        seqElem.appendChild(li);
    });
});

connection.on("advisoryRaised", adv => {
    // Optionally handle advisories (e.g., show alert)
    alert("Advisory: " + JSON.stringify(adv));
});

connection.start().catch(err => console.error(err));
