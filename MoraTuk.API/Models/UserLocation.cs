namespace MoraTuk.API.Models;

public class UserLocation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}