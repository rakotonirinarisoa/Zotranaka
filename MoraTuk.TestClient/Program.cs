using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using MoraTuk.Mobile.Models;

var configUrl =
    "https://wind-arrangement-wagon-horses.trycloudflare.com/api/config";

try
{
    // Récupération de la configuration
    using var httpClient = new HttpClient();

    var config = await httpClient
        .GetFromJsonAsync<AppConfig>(configUrl);

    if (config == null || string.IsNullOrWhiteSpace(config.ApiUrl))
    {
        Console.WriteLine("❌ Impossible de récupérer l'URL de l'API.");
        return;
    }

    var apiUrl = config.ApiUrl.TrimEnd('/');

    Console.WriteLine($"🌐 API : {apiUrl}");

    // Connexion SignalR
    var connection = new HubConnectionBuilder()
        .WithUrl($"{apiUrl}/tracking")
        .WithAutomaticReconnect()
        .Build();

    // Réception d'une nouvelle course
    connection.On("NewRide", ride =>
    {
        Console.WriteLine("🚕 Nouvelle course reçue !");
        Console.WriteLine(ride);
    });

    // Connexion
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
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erreur : {ex.Message}");
}