namespace MoraTuk.Mobile.Models;

public class FleetLocationDto
{
    public int DriverId { get; set; }

    public string? VehicleNumber { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime? LastUpdate { get; set; }

    public double Speed { get; set; }
}