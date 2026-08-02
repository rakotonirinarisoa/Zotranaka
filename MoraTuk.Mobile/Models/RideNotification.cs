namespace MoraTuk.Mobile.Models;

public class RideNotification
{
    public int RideId { get; set; }

    public double PickupLatitude { get; set; }

    public double PickupLongitude { get; set; }

    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }

    public decimal Price { get; set; }
    public string RideType { get; set; } = "";

    public int Passengers { get; set; }

    public double DistanceToDriver { get; set; }
}