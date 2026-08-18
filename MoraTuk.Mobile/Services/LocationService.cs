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
            Location? bestLocation = null;

            for (int i = 0; i < 8; i++)
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(10)); // timeout plus long

                var location = await Geolocation.GetLocationAsync(request);
                if (location == null) continue;

                Console.WriteLine(
                    $"GPS TENTATIVE {i} : Lat={location.Latitude}, " +
                    $"Lon={location.Longitude}, Précision={location.Accuracy}m");

                if (bestLocation == null ||
                    (location.Accuracy.HasValue &&
                    (!bestLocation.Accuracy.HasValue ||
                    location.Accuracy.Value < bestLocation.Accuracy.Value)))
                {
                    bestLocation = location;
                }

                // on accepte seulement une précision correcte
                if (bestLocation?.Accuracy <= 30)
                    break;

                await Task.Delay(2000); // laisser le GPS "respirer"
            }

            if (bestLocation == null)
            {
                Console.WriteLine("GPS : aucune position obtenue après 8 tentatives.");
                return null;
            }

            // On renvoie toujours la meilleure position trouvée, même imparfaite
            // (seuil strict de 50m trop restrictif en usage réel : émulateur,
            // intérieur, zones urbaines denses)
            if (bestLocation.Accuracy is > 100)
            {
                Console.WriteLine($"GPS imprécis mais utilisé quand même : {bestLocation.Accuracy}m");
            }

            return bestLocation;
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
