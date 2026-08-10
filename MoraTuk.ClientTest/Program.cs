using Microsoft.AspNetCore.SignalR.Client;


// var connection = new HubConnectionBuilder()
//     .WithUrl("http://localhost:5078/tracking")
//     .WithAutomaticReconnect()
//     .Build();
var connection = new HubConnectionBuilder()
    .WithUrl("https://volunteer-filme-focus-westminster.trycloudflare.com/tracking")
    .WithAutomaticReconnect()
    .Build();

// Réception acceptation course
connection.On<object>("RideAccepted", data =>
{
    Console.WriteLine("🚕 Votre course est acceptée !");
    Console.WriteLine(data);
});


// Réception position chauffeur
connection.On<object>(
    "DriverLocationUpdated",
    location =>
    {
        Console.WriteLine("📍 Position chauffeur reçue !");
        Console.WriteLine(location);
    });


await connection.StartAsync();

Console.WriteLine("✅ Client connecté SignalR");


// Client = utilisateur Id 2
await connection.InvokeAsync(
    "RegisterClient",
    2
);


Console.WriteLine("👤 Client enregistré");
Console.WriteLine("En attente...");

Console.ReadLine();