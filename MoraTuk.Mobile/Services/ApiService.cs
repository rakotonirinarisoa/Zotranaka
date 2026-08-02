using System.Net.Http.Json;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService()
    {
        // _http = new HttpClient
        // {
        //     BaseAddress = new Uri("http://localhost:5078")
        // };
        _http = new HttpClient
            {
                BaseAddress = new Uri("http://192.168.1.106:5078")
            };
    }

    public async Task<string> TestApi()
    {
        var response = await _http.GetAsync("/swagger/index.html");

        return response.IsSuccessStatusCode
            ? "API OK"
            : "API erreur";
    }
     public async Task<LoginResponse?> LoginAsync(LoginDto dto)
    {
        var response = await _http.PostAsJsonAsync("/api/Auth/login", dto);
        var content = await response.Content.ReadAsStringAsync();
        await Application.Current.MainPage.DisplayAlert(
            "API Response",
            content,
            "OK");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<LoginResponse>();
    }
    public async Task<PriceEstimateResponse?> EstimatePriceAsync(
    CreateRideDto dto)
    {
        var response = await _http.PostAsJsonAsync(
            "api/Ride/estimate",
            dto);


        if(!response.IsSuccessStatusCode)
            return null;


        return await response.Content
            .ReadFromJsonAsync<PriceEstimateResponse>();
    }
    public async Task<int> GetDriverIdAsync(int userId)
        {
            var response = await _http
                .GetFromJsonAsync<DriverDto>(
                    $"api/Driver/by-user/{userId}");

            if (response == null)
            {
                throw new Exception(
                    "Profil chauffeur introuvable");
            }

            return response.DriverId;
        }
}