namespace MoraTuk.Mobile.Models;

public class RideNotification
{
    public int RideId { get; set; }
    public int? DriverId { get; set; }

    // Nom du départ
    public string Departure { get; set; } = "";

    // Coordonnées départ
    public double PickupLatitude { get; set; }

    public double PickupLongitude { get; set; }

    // Nom destination
    public string Destination { get; set; } = "";

    // Coordonnées destination
    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }

    public decimal Price { get; set; }

    public string RideType { get; set; } = "";

    public int Passengers { get; set; }

    public double DistanceToDriver { get; set; }
    
    // ============================================================
    // STATUT DE LA COURSE
    // ============================================================

    public string Status { get; set; } = "";
}

