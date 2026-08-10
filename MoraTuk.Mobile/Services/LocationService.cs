using Microsoft.Maui.Devices.Sensors;
using System.Net.Http.Json;
using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class LocationService
{
    private readonly HttpClient _http;

    public LocationService()
    {
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    // ============================================================
    // URL CENTRALE
    // ============================================================

    private string BaseUrl =>
        ApiSettings.BaseUrl.TrimEnd('/');

    // ============================================================
    // GPS ACTUEL
    // ============================================================

   public async Task<Location?> GetCurrentLocation()
{
    try
    {
        var request = new GeolocationRequest(
            GeolocationAccuracy.Best,
            TimeSpan.FromSeconds(15));

        var location =
            await Geolocation.GetLocationAsync(request);

        if (location == null)
        {
            Console.WriteLine("GPS : NULL");
            return null;
        }

        Console.WriteLine(
            $"GPS : Lat={location.Latitude:F6}, " +
            $"Lon={location.Longitude:F6}, " +
            $"Accuracy={location.Accuracy} m");

        // Position trop imprécise
        if (location.Accuracy.HasValue &&
            location.Accuracy.Value > 100)
        {
            Console.WriteLine(
                $"GPS trop imprécis : {location.Accuracy.Value} m");

            return null;
        }

        return location;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"GPS ERROR : {ex}");
        return null;
    }
}
    // ============================================================
    // ENREGISTRER POSITION UTILISATEUR
    // ============================================================

    public async Task SaveUserLocationAsync(
        int userId,
        double latitude,
        double longitude)
    {
        try
        {
            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            if (userId <= 0)
            {
                throw new Exception(
                    $"UserId invalide : {userId}");
            }

            var dto = new UserLocationDto
            {
                UserId = userId,
                Latitude = latitude,
                Longitude = longitude
            };

            var url =
                $"{BaseUrl}/api/UserLocations";

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                $"SAVE LOCATION URL : {url}");

            Console.WriteLine(
                $"UserId : {userId}");

            Console.WriteLine(
                $"Latitude : {latitude}");

            Console.WriteLine(
                $"Longitude : {longitude}");

            Console.WriteLine(
                "====================================");

            var response =
                await _http.PostAsJsonAsync(
                    url,
                    dto);

            var responseBody =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"SAVE LOCATION STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"SAVE LOCATION RESPONSE : " +
                responseBody);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Impossible d'enregistrer la position.\n\n" +
                    $"HTTP : {(int)response.StatusCode}\n" +
                    $"Réponse : {responseBody}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"SAVE LOCATION ERROR : {ex}");

            throw;
        }
    }

    // ============================================================
    // DERNIÈRE POSITION UTILISATEUR
    // ============================================================

    public async Task<UserLocationDto?> GetLastLocationAsync(
        int userId)
    {
        try
        {
            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            if (userId <= 0)
            {
                throw new Exception(
                    $"UserId invalide : {userId}");
            }

            var url =
                $"{BaseUrl}/api/UserLocations/last/{userId}";

            Console.WriteLine(
                $"GET LAST LOCATION : {url}");

            var response =
                await _http.GetAsync(url);

            var responseBody =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"LAST LOCATION STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"LAST LOCATION RESPONSE : " +
                responseBody);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return null;
            }

            return
                System.Text.Json.JsonSerializer
                    .Deserialize<UserLocationDto>(
                        responseBody,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"GET LAST LOCATION ERROR : {ex}");

            return null;
        }
    }
}