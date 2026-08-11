namespace MoraTuk.API.Models
{
    public class CreateRideRequest
    {
        public int ClientId { get; set; }

        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public string Departure { get; set; } = "";

        public double DestinationLatitude { get; set; }
        public double DestinationLongitude { get; set; }
        public string Destination { get; set; } = "";

        public string RideType { get; set; } = "Shared";

        // Numéro MVola du client
        public string DebitMsisdn { get; set; } = "";
    }
}