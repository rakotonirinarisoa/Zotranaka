using System.Net.Http.Json;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class LocationSearchService
{
    private readonly HttpClient _http;


    public LocationSearchService(HttpClient http)
    {
        _http = http;
    }
    public string LastResponse { get; private set; } = "";


    public async Task<List<LocationDto>> SearchAsync(string text)
    {
        try
        {
            var url =
                $"api/locations/search?text={Uri.EscapeDataString(text)}";


            var response =
                await _http.GetAsync(url);


            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"API erreur : {response.StatusCode}");
            }


            var result =
                await response.Content
                .ReadFromJsonAsync<List<LocationDto>>();


            return result ?? new List<LocationDto>();
        }
        catch(Exception ex)
        {
            throw new Exception(
                "Recherche impossible : " + ex.Message);
        }
    }
    public async Task<LocationDto?> GetNearestPlace(
    double latitude,
    double longitude)
{
    try
    {
        if (_http == null)
        {
            await Shell.Current.DisplayAlert(
                "Erreur",
                "_http est NULL",
                "OK");

            return null;
        }
        var url =
            $"api/locations/nearest?latitude={latitude}&longitude={longitude}";


        var response =
            await _http.GetAsync(url);


        var content =
            await response.Content.ReadAsStringAsync();
        LastResponse = content;
        
        await Application.Current.MainPage.DisplayAlert(
            "JSON reçu",
            content,
            "OK");

        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlert(
                "API Debug",
                $"Status : {response.StatusCode}\n\n{content}",
                "OK");
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }


        var result =
            System.Text.Json.JsonSerializer
            .Deserialize<NearestLocationDto>(
                content,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });


        return result?.Location;

    }
    catch(Exception ex)
    {
        await Shell.Current.DisplayAlert(
            "Erreur",
            ex.ToString(),
            "OK");

        return null;
    }
}
}