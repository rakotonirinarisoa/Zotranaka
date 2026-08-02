using System.Text.Json.Serialization;

namespace MoraTuk.Mobile.Models;

public class PlaceResult
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("lat")]
    public string Latitude { get; set; } = "";

    [JsonPropertyName("lon")]
    public string Longitude { get; set; } = "";
}