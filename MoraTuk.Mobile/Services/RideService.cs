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

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Erreur création course\n\n" +
                    $"HTTP : {(int)response.StatusCode}\n" +
                    $"Message : {responseBody}");
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new Exception(
                    "L'API a retourné une réponse vide.");
            }

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


    // ============================================================
    // COURSES ACTIVES DU CHAUFFEUR
    // ============================================================

    public async Task<List<RideNotification>>
        GetAvailableRidesAsync(int driverId)
    {
        try
        {
            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            var url =
                $"{BaseUrl}/api/Ride/available/{driverId}";

            Console.WriteLine(
                $"ACTIVE RIDES URL : {url}");

            var response =
                await _http.GetAsync(url);

            var content =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"ACTIVE RIDES STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"ACTIVE RIDES RESPONSE : " +
                content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Erreur API {(int)response.StatusCode} : " +
                    content);
            }

            var rides =
                JsonSerializer.Deserialize<
                    List<RideNotification>>(
                        content,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

            return rides ??
                new List<RideNotification>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR GetAvailableRidesAsync : {ex}");

            throw;
        }
    }


    // ============================================================
    // ACCEPTER COURSE
    // ============================================================

    public async Task<bool> AcceptRideAsync(
        int rideId,
        int driverId)
    {
        try
        {
            var url =
                $"{BaseUrl}/api/Ride/{rideId}" +
                $"/accept?driverId={driverId}";

            Console.WriteLine(
                $"ACCEPT RIDE URL : {url}");

            var response =
                await _http.PutAsync(
                    url,
                    null);

            var content =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"ACCEPT RIDE STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"ACCEPT RIDE RESPONSE : " +
                content);

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


    // ============================================================
    // TERMINER COURSE
    // ============================================================

    public async Task<bool> CompleteRideAsync(
        int rideId,
        int driverId)
    {
        try
        {
            var url =
                $"{BaseUrl}/api/Ride/{rideId}" +
                $"/complete?driverId={driverId}";

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                $"COMPLETE RIDE URL : {url}");

            Console.WriteLine(
                $"RideId   : {rideId}");

            Console.WriteLine(
                $"DriverId : {driverId}");

            Console.WriteLine(
                "====================================");

            var response =
                await _http.PutAsync(
                    url,
                    null);

            var content =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"COMPLETE RIDE STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"COMPLETE RIDE RESPONSE : " +
                content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"COMPLETE RIDE ERROR : {content}");

                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR CompleteRideAsync : {ex}");

            return false;
        }
    }
}