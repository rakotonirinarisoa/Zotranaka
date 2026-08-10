using System.Net.Http.Json;
using System.Text.Json;
using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class RideService
{
    private readonly HttpClient _http;

    public RideService()
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
    // CRÉER UNE COURSE
    // ============================================================

    public async Task<RideResponse?> CreateRideAsync(
        CreateRideDto dto)
    {
        try
        {
            if (dto == null)
            {
                throw new Exception(
                    "CreateRideDto est NULL.");
            }

            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            var url =
                $"{BaseUrl}/api/Ride/create";

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                $"CREATE RIDE URL : {url}");

            Console.WriteLine(
                $"ClientId : {dto.ClientId}");

            Console.WriteLine(
                $"Pickup : " +
                $"{dto.PickupLatitude}, " +
                $"{dto.PickupLongitude}");

            Console.WriteLine(
                $"Destination : " +
                $"{dto.DestinationLatitude}, " +
                $"{dto.DestinationLongitude}");

            Console.WriteLine(
                $"RideType : {dto.RideType}");

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
                $"CREATE RIDE STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"CREATE RIDE RESPONSE : " +
                responseBody);

            // ====================================================
            // ERREUR API
            // ====================================================

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Erreur création course\n\n" +
                    $"HTTP : {(int)response.StatusCode}\n" +
                    $"Message : {responseBody}");
            }

            // ====================================================
            // RÉPONSE VIDE
            // ====================================================

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new Exception(
                    "L'API a retourné une réponse vide.");
            }

            // ====================================================
            // DÉSÉRIALISATION
            // ====================================================

            var result =
                JsonSerializer.Deserialize<RideResponse>(
                    responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                throw new Exception(
                    "Impossible de convertir la réponse " +
                    "en RideResponse.");
            }

            Console.WriteLine(
                $"COURSE CRÉÉE : {result.Id}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR CreateRideAsync : {ex}");

            throw;
        }
    }
   public async Task<List<RideNotification>> GetAvailableRidesAsync(
    int driverId)
{
    try
    {
        if (!ApiSettings.IsConfigured)
        {
            throw new Exception(
                "ApiSettings.BaseUrl n'est pas configuré.");
        }

        var url =
            $"{ApiSettings.BaseUrl.TrimEnd('/')}" +
            $"/api/Ride/available/{driverId}";

        Console.WriteLine(
            $"AVAILABLE RIDES URL : {url}");

        var response =
            await _http.GetAsync(url);

        var content =
            await response.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"AVAILABLE RIDES STATUS : {(int)response.StatusCode}");

        Console.WriteLine(
            $"AVAILABLE RIDES RESPONSE : {content}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Erreur API {(int)response.StatusCode} : {content}");
        }

        var rides =
            JsonSerializer.Deserialize<List<RideNotification>>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return rides ?? new List<RideNotification>();
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"ERREUR GetAvailableRidesAsync : {ex}");

        throw;
    }
}
public async Task<bool> AcceptRideAsync(
    int rideId,
    int driverId)
{
    try
    {
        var url =
            $"{ApiSettings.BaseUrl.TrimEnd('/')}" +
            $"/api/Ride/{rideId}/accept?driverId={driverId}";

        Console.WriteLine(
            $"ACCEPT RIDE URL : {url}");

        var response =
            await _http.PutAsync(
                url,
                null);

        var content =
            await response.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"ACCEPT RIDE STATUS : {(int)response.StatusCode}");

        Console.WriteLine(
            $"ACCEPT RIDE RESPONSE : {content}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(
                $"ACCEPT RIDE ERROR : {content}");

            return false;
        }

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"ERREUR AcceptRideAsync : {ex}");

        return false;
    }
}
}