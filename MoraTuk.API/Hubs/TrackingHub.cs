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
    // Quand un chauffeur se connecte
    public async Task RegisterDriver(int driverId)
    {
        var groupName = $"driver-{driverId}";

        Console.WriteLine(
        $"Chauffeur ajouté au groupe driver-{driverId}");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName);
        Console.WriteLine(
        $"Connexion {Context.ConnectionId} ajoutée au groupe {groupName}");
    }

    // Quand un client se connecte
    public async Task RegisterClient(int clientId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"client-{clientId}");
    }

    // Position GPS du chauffeur
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
    public async Task AcceptRide(int rideId, int driverId)
    {
         var ride = await _context.Rides
        .FirstOrDefaultAsync(x => x.Id == rideId);

        if (ride == null)
        {
            return;
        }
        await Clients.Group($"client-{ride.ClientId}")
        .SendAsync(
            "RideAccepted",
            new
            {
                rideId,
                driverId,
                status = "Accepted"
            });
            
    }
    public async Task UpdateDriverLocation(
    int driverId,
    int clientId,
    double latitude,
    double longitude)
    {
         var driver = await _context.Drivers
        .FirstOrDefaultAsync(d => d.Id == driverId);

        if(driver != null)
        {
            driver.Latitude = latitude;
            driver.Longitude = longitude;
            driver.LastUpdate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        await Clients.Group($"client-{clientId}")
        .SendAsync(
            "DriverLocationUpdated",
            new
            {
                driverId,
                latitude,
                longitude
            });
    }
}