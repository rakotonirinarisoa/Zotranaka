namespace MoraTuk.API.Models;

public class DriverLocation
{
    public int Id { get; set; }

    public int DriverId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}