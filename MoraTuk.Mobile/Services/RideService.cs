using System.Net.Http.Json;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class RideService
{
    private readonly HttpClient _http;

    public RideService(HttpClient http)
    {
        _http = http;
    }


    public async Task<RideResponse?> CreateRideAsync(
        CreateRideDto dto)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/Ride/create",
            dto);


        // if (!response.IsSuccessStatusCode)
        // {
        //     return null;
        // }
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new Exception(
                $"Code: {response.StatusCode}\n{error}"
            );
        }


        return await response.Content
            .ReadFromJsonAsync<RideResponse>();
    }
}