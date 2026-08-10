using System.Net.Http.Json;
using System.Text.Json;
using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class LocationSearchService
{
    private readonly HttpClient _http;

    public string LastResponse { get; private set; } = "";

    public LocationSearchService()
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
    // RECHERCHE DESTINATION
    // ============================================================

    public async Task<List<LocationDto>> SearchAsync(
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<LocationDto>();
        }

        try
        {
            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            var url =
                $"{BaseUrl}/api/Locations/search" +
                $"?text={Uri.EscapeDataString(text)}";

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                $"LOCATION SEARCH URL : {url}");

            Console.WriteLine(
                $"TEXT : {text}");

            Console.WriteLine(
                "====================================");

            var response =
                await _http.GetAsync(url);

            var content =
                await response.Content
                    .ReadAsStringAsync();

            LastResponse = content;

            Console.WriteLine(
                $"LOCATION HTTP : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"LOCATION RESPONSE : " +
                content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"API erreur {(int)response.StatusCode}\n\n" +
                    content);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<LocationDto>();
            }

            var result =
                JsonSerializer.Deserialize<
                    List<LocationDto>>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return result ??
                   new List<LocationDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"LOCATION SEARCH ERROR : {ex}");

            throw;
        }
    }

    // ============================================================
    // LIEU LE PLUS PROCHE
    // ============================================================

    public async Task<LocationDto?> GetNearestPlace(
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

            var url =
                $"{BaseUrl}/api/Locations/nearest" +
                $"?latitude={latitude}" +
                $"&longitude={longitude}";

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                $"NEAREST URL : {url}");

            Console.WriteLine(
                $"Latitude : {latitude}");

            Console.WriteLine(
                $"Longitude : {longitude}");

            Console.WriteLine(
                "====================================");

            var response =
                await _http.GetAsync(url);

            var content =
                await response.Content
                    .ReadAsStringAsync();

            LastResponse = content;

            Console.WriteLine(
                $"NEAREST HTTP : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"NEAREST RESPONSE : " +
                content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var result =
                JsonSerializer.Deserialize<
                    NearestLocationDto>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return result?.Location;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"NEAREST ERROR : {ex}");

            throw;
        }
    }
}