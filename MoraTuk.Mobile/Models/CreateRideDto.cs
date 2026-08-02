namespace MoraTuk.Mobile.Models;

public class CreateRideDto
{
    public int ClientId { get; set; }

    public string Departure { get; set; } = "";

    public double PickupLatitude { get; set; }

    public double PickupLongitude { get; set; }


    public string Destination { get; set; } = "";

    public double DestinationLatitude { get; set; }

    public double DestinationLongitude { get; set; }


    //public decimal Price { get; set; }
    public string RideType { get; set; } = "Shared";

    public string Status { get; set; } = "Pending";
}