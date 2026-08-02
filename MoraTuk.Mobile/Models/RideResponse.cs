namespace MoraTuk.Mobile.Models;

public class RideResponse
{
    public int Id { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; } = "";
    public string Driver { get; set; } = "";
}