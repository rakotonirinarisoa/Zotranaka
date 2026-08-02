using Microsoft.Maui.Devices.Sensors;
using System.Net.Http.Json;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class LocationService
{
    private readonly HttpClient _http;


    public LocationService(HttpClient http)
    {
        _http = http;
    }
    public async Task<Location?> GetCurrentLocation()
    {
        try
        {
            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.High,
                    Timeout = TimeSpan.FromSeconds(10)
                });

            return location;
        }
        catch
        {
            return null;
        }
    }
    public async Task SaveUserLocationAsync(
    int userId,
    double latitude,
    double longitude)
    {
        var dto = new UserLocationDto
        {
            UserId = userId,
            Latitude = latitude,
            Longitude = longitude
        };


        var response = await _http.PostAsJsonAsync(
            "api/UserLocations",
            dto);


        if(!response.IsSuccessStatusCode)
        {
            throw new Exception(
                "Impossible d'enregistrer la position");
        }
    }
    public async Task<UserLocationDto?> GetLastLocationAsync(int userId)
    {
        var response = await _http.GetAsync(
            $"api/UserLocations/last/{userId}");

        if(!response.IsSuccessStatusCode)
            return null;


        return await response.Content
            .ReadFromJsonAsync<UserLocationDto>();
    }
}