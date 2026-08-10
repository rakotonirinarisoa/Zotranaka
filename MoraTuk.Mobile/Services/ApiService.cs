using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService()
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
    // TEST API
    // ============================================================

    public async Task<string> TestApi()
    {
        try
        {
            var url = $"{BaseUrl}/swagger/index.html";

            var response = await _http.GetAsync(url);

            return response.IsSuccessStatusCode
                ? "API OK"
                : $"API erreur : {(int)response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Connection failure : {ex.Message}";
        }
    }

    // ============================================================
    // LOGIN
    // ============================================================

    public async Task<LoginResponse?> LoginAsync(LoginDto dto)
    {
        try
        {
            var url = $"{BaseUrl}/api/Auth/login";

            Console.WriteLine($"LOGIN URL : {url}");

            var response =
                await _http.PostAsJsonAsync(url, dto);

            var content =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"LOGIN STATUS : {(int)response.StatusCode}");

            Console.WriteLine(
                $"LOGIN RESPONSE : {content}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return JsonSerializer.Deserialize<LoginResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"LOGIN ERROR : {ex}");

            throw;
        }
    }

    // ============================================================
    // ESTIMATION PRIX
    // ============================================================

    public async Task<PriceEstimateResponse?> EstimatePriceAsync(
        CreateRideDto dto)
    {
        try
        {
            var mainPage =
                Application.Current?.MainPage;

            if (mainPage == null)
                throw new Exception(
                    "MainPage est NULL.");

            await mainPage.DisplayAlert(
                "DEBUG PRIX",
                "1. Entrée dans EstimatePriceAsync",
                "OK");

            if (dto == null)
            {
                await mainPage.DisplayAlert(
                    "DEBUG PRIX",
                    "2. DTO = NULL",
                    "OK");

                return null;
            }

            await mainPage.DisplayAlert(
                "DEBUG PRIX",
                $"2. DTO OK\n\n" +
                $"ClientId : {dto.ClientId}\n" +
                $"Pickup : {dto.PickupLatitude}, {dto.PickupLongitude}\n" +
                $"Destination : {dto.DestinationLatitude}, {dto.DestinationLongitude}",
                "OK");

            await mainPage.DisplayAlert(
                "DEBUG PRIX",
                $"3. Base URL :\n{ApiSettings.BaseUrl}",
                "OK");

            if (!ApiSettings.IsConfigured)
            {
                await mainPage.DisplayAlert(
                    "DEBUG PRIX",
                    "BaseUrl est vide.",
                    "OK");

                return null;
            }

            var url =
                $"{BaseUrl}/api/Ride/estimate";

            await mainPage.DisplayAlert(
                "DEBUG PRIX",
                $"4. URL appelée :\n{url}",
                "OK");

            var json =
                JsonSerializer.Serialize(dto);

            await mainPage.DisplayAlert(
                "DEBUG PRIX",
                $"5. JSON :\n{json}",
                "OK");

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            await mainPage.DisplayAlert(
                "DEBUG PRIX",
                "6. Envoi vers API...",
                "OK");

            var response =
                await _http.PostAsync(
                    url,
                    content);

            var responseBody =
                await response.Content
                    .ReadAsStringAsync();

            await mainPage.DisplayAlert(
                "DEBUG PRIX",
                $"7. HTTP : {(int)response.StatusCode}\n\n" +
                responseBody,
                "OK");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"API {(int)response.StatusCode} : {responseBody}");
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new Exception(
                    "L'API a retourné une réponse vide.");
            }

            var result =
                JsonSerializer.Deserialize<PriceEstimateResponse>(
                    responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                throw new Exception(
                    "Impossible de convertir la réponse.");
            }

            await mainPage.DisplayAlert(
                "DEBUG PRIX",
                $"8. SUCCÈS\n\n" +
                $"Distance : {result.DistanceKm:F2} km\n" +
                $"Prix : {result.Price:F0} Ar",
                "OK");

            return result;
        }
        catch (Exception ex)
        {
            var message =
                $"TYPE : {ex.GetType().Name}\n\n" +
                $"MESSAGE : {ex.Message}\n\n" +
                $"STACK : {ex.StackTrace}";

            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "ERREUR EstimatePriceAsync",
                    message,
                    "OK");
            }

            return null;
        }
    }

    // ============================================================
    // CHAUFFEUR PAR USER ID
    // ============================================================

    public async Task<int> GetDriverIdAsync(int userId)
    {
        var url =
            $"{BaseUrl}/api/Driver/by-user/{userId}";

        Console.WriteLine(
            $"GET DRIVER URL : {url}");

        var response =
            await _http.GetAsync(url);

        var body =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Erreur récupération chauffeur : " +
                $"{(int)response.StatusCode}\n{body}");
        }

        var driver =
            JsonSerializer.Deserialize<DriverDto>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (driver == null)
        {
            throw new Exception(
                "Profil chauffeur introuvable.");
        }

        return driver.DriverId;
    }
}