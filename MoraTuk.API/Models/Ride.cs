namespace MoraTuk.API.Models;

public class Ride
{
    public int Id { get; set; }


    // Client qui commande
    public int ClientId { get; set; }

    public User? Client { get; set; }

    // Chauffeur affecté
    public int? DriverId { get; set; }

    public Driver? Driver { get; set; }

    // Point de départ
    public double PickupLatitude { get; set; }

    public double PickupLongitude { get; set; }

    // Destination
    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }

    // Prix
    public decimal Price { get; set; }

    public string RideType { get; set; } = "Shared";
    // Shared ou Private
    public int MaxPassengers { get; set; } = 4;

    public int CurrentPassengers { get; set; } = 1;
    // Statut de la course
    public string Status { get; set; } = "Pending";


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}