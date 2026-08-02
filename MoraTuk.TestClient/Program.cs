using Microsoft.AspNetCore.SignalR.Client;


var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5078/tracking")
    .WithAutomaticReconnect()
    .Build();


// Réception d'une nouvelle course
connection.On<object>("NewRide", ride =>
{
    Console.WriteLine("🚕 Nouvelle course reçue !");
    Console.WriteLine(ride);
});


// Connexion
try
{
    await connection.StartAsync();

    Console.WriteLine("✅ Connecté au serveur SignalR");

    // Simulation chauffeur Jean (id = 1)
    await connection.InvokeAsync(
        "RegisterDriver",
        1
    );

    Console.WriteLine("🚕 Chauffeur enregistré");
    while (true)
    {
        await connection.InvokeAsync(
            "UpdateDriverLocation",
            1,          // driverId
            2,          // clientId
            -18.8792,   // latitude
            47.5079     // longitude
        );

        Console.WriteLine("📍 Position envoyée");

        await Task.Delay(5000);
    }

    Console.WriteLine("En attente des courses...");

    Console.ReadLine();
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}