using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;

namespace MoraTuk.API.Hubs;

public class TrackingHub : Hub
{
private readonly AppDbContext _context;

public TrackingHub(AppDbContext context)
{
    _context = context;
}

// ============================================================
// ENREGISTRER UN CHAUFFEUR
// ============================================================

public async Task RegisterDriver(int driverId)
{
    if (driverId <= 0)
    {
        Console.WriteLine(
            "RegisterDriver : driverId invalide.");

        return;
    }

    var groupName = $"driver-{driverId}";

    Console.WriteLine(
        "========================================");

    Console.WriteLine(
        $"REGISTER DRIVER");

    Console.WriteLine(
        $"DriverId : {driverId}");

    Console.WriteLine(
        $"Group : {groupName}");

    Console.WriteLine(
        $"ConnectionId : {Context.ConnectionId}");

    Console.WriteLine(
        "========================================");

    await Groups.AddToGroupAsync(
        Context.ConnectionId,
        groupName);

    Console.WriteLine(
        $"Chauffeur {driverId} ajouté au groupe {groupName}.");
}

// ============================================================
// ENREGISTRER UN CLIENT
// ============================================================

public async Task RegisterClient(int clientId)
{
    if (clientId <= 0)
        return;

    var groupName = $"client-{clientId}";

    await Groups.AddToGroupAsync(
        Context.ConnectionId,
        groupName);

    Console.WriteLine(
        $"Client {clientId} ajouté au groupe {groupName}.");
}

// ============================================================
// POSITION GPS DU CHAUFFEUR
// ============================================================

public async Task SendLocation(
    int driverId,
    double latitude,
    double longitude)
{
    await Clients.All.SendAsync(
        "DriverLocation",
        driverId,
        latitude,
        longitude);
}

// ============================================================
// ACCEPTER UNE COURSE
// ============================================================

public async Task AcceptRide(
    int rideId,
    int driverId)
{
    var ride = await _context.Rides
        .FirstOrDefaultAsync(
            x => x.Id == rideId);

    if (ride == null)
    {
        Console.WriteLine(
            $"AcceptRide : course {rideId} introuvable.");

        return;
    }

    await Clients
        .Group($"client-{ride.ClientId}")
        .SendAsync(
            "RideAccepted",
            new
            {
                rideId = ride.Id,
                driverId = driverId,
                status = "Accepted"
            });

    Console.WriteLine(
        $"Course {rideId} acceptée par chauffeur {driverId}.");
}

// ============================================================
// POSITION DU CHAUFFEUR
// ============================================================

public async Task UpdateDriverLocation(
    int driverId,
    int clientId,
    double latitude,
    double longitude)
{
    var driver = await _context.Drivers
        .FirstOrDefaultAsync(
            d => d.Id == driverId);

    if (driver != null)
    {
        driver.Latitude = latitude;
        driver.Longitude = longitude;
        driver.LastUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    await Clients
        .Group($"client-{clientId}")
        .SendAsync(
            "DriverLocationUpdated",
            new
            {
                driverId,
                latitude,
                longitude
            });
}

// ============================================================
// DÉCONNEXION
// ============================================================

public override async Task OnDisconnectedAsync(
    Exception? exception)
{
    Console.WriteLine(
        $"SignalR déconnecté : {Context.ConnectionId}");

    if (exception != null)
    {
        Console.WriteLine(
            $"Cause : {exception.Message}");
    }

    await base.OnDisconnectedAsync(exception);
}

}
