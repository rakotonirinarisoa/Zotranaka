namespace MoraTuk.Mobile.Models;

public class FleetLocationsResponse
{
    public bool Success { get; set; }

    public int Total { get; set; }

    public List<FleetLocationDto> Drivers { get; set; }
        = new();
}