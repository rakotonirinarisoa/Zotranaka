using System.Text.Json;
using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class ConfigService
{
    private readonly HttpClient _httpClient;

    // URL FIXE
    // Cette URL ne change PAS quand Cloudflare change.
    private const string ConfigUrl =
        "https://raw.githubusercontent.com/rakotonirinarisoa/MoraTuk-Config/main/config.json";

    public ConfigService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<bool> LoadConfigAsync()
    {
        try
        {
            Console.WriteLine(
                $"Chargement configuration : {ConfigUrl}");

            var response =
                await _httpClient.GetAsync(ConfigUrl);

            var content =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"Config HTTP : {(int)response.StatusCode}");

            Console.WriteLine(
                $"Config response : {content}");

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var config =
                JsonSerializer.Deserialize<AppConfig>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (config == null ||
                string.IsNullOrWhiteSpace(config.ApiUrl))
            {
                return false;
            }

            ApiSettings.BaseUrl =
                config.ApiUrl.TrimEnd('/');

            Console.WriteLine(
                $"API configurée : {ApiSettings.BaseUrl}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Erreur ConfigService : {ex}");

            return false;
        }
    }
}