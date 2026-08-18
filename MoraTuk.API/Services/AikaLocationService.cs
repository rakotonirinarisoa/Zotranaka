using System.Text.Json;
using System.Xml.Linq;

namespace MoraTuk.API.Services;

public class AikaLocationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private const string AppKey = "7DU2DJFDR8321";

    public AikaLocationService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AikaLocationResult?> GetTrackingAsync(
    int deviceId,
    string username,
    string password)
    {
        var apiUrl = _configuration["Aika:ApiUrl"];
        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new Exception("Aika:ApiUrl est manquant.");

        if (string.IsNullOrWhiteSpace(username))
        throw new Exception("Identifiant AIKA manquant.");

        if (string.IsNullOrWhiteSpace(password))
            throw new Exception("Mot de passe AIKA manquant.");
        // ============================================================
        // LOGIN AIKA
        // ============================================================

        var loginData = new Dictionary<string, string>
        {
            ["Name"] = username,
            ["Pass"] = password,
            ["LoginType"] = "1",
            ["LoginAPP"] = "AKSH",
            ["GMT"] = "2:00",
            ["Key"] = AppKey
        };

        using var loginContent =
            new FormUrlEncodedContent(loginData);

        var loginResponse = await _httpClient.PostAsync(
            $"{apiUrl}/Login",
            loginContent);

        loginResponse.EnsureSuccessStatusCode();

        var loginText =
            await loginResponse.Content.ReadAsStringAsync();

        var loginJson =
            ExtractJson(loginText);

        using var loginDocument =
            JsonDocument.Parse(loginJson);

        var root = loginDocument.RootElement;

        var state = root
            .GetProperty("state")
            .GetString();

        if (state != "0")
        {
            throw new Exception(
                $"Connexion AIKA échouée. State={state}");
        }

        if (!root.TryGetProperty("deviceInfo", out var deviceInfo))
        {
            throw new Exception(
                "AIKA n'a pas retourné deviceInfo.");
        }

        var key = deviceInfo
            .GetProperty("key2018")
            .GetString();

        var model = int.TryParse(
            deviceInfo.GetProperty("model").ToString(),
            out var parsedModel)
            ? parsedModel
            : 0;

        // ============================================================
        // GET TRACKING
        // ============================================================

        var trackingData = new Dictionary<string, string>
        {
            ["DeviceID"] = deviceId.ToString(),
            ["Model"] = model.ToString(),
            ["TimeZones"] = "2:00",
            ["MapType"] = "Google",
            ["Language"] = "en",
            ["Key"] = key ?? ""
        };

        using var trackingContent =
            new FormUrlEncodedContent(trackingData);

        var trackingResponse = await _httpClient.PostAsync(
            $"{apiUrl}/GetTracking",
            trackingContent);

        trackingResponse.EnsureSuccessStatusCode();

        var trackingText =
            await trackingResponse.Content.ReadAsStringAsync();

        var trackingJson =
            ExtractJson(trackingText);

        using var trackingDocument =
            JsonDocument.Parse(trackingJson);

        var tracking =
            trackingDocument.RootElement;

        var trackingState =
            tracking.GetProperty("state").GetString();

        if (trackingState != "0")
        {
            return null;
        }

        // ============================================================
        // RESULTAT GPS
        // ============================================================

        var latitude =
            GetDouble(tracking, "lat");

        var longitude =
            GetDouble(tracking, "lng");

        var speed =
            GetDouble(tracking, "speed");

        var course =
            GetDouble(tracking, "course");

        var isGps =
            GetInt(tracking, "isGPS") == 1;

        var isStop =
            GetInt(tracking, "isStop") == 1;
        var battery =
            GetInt(tracking, "battery");

        var status =
            GetString(tracking, "status");

        var positionTime =
            GetString(tracking, "positionTime");

       return new AikaLocationResult
        {
            DeviceId = deviceId,
            Latitude = latitude,
            Longitude = longitude,
            Speed = speed,
            Course = course,
            IsGps = isGps,
            IsStopped = isStop,
            Battery = battery,
            Status = status,
            PositionTime = positionTime
        };
    }

    private static string ExtractJson(string response)
    {
        // Certains endpoints AIKA retournent le JSON
        // à l'intérieur d'une réponse XML.

        try
        {
            var xml = XDocument.Parse(response);

            var text = xml
                .Descendants()
                .Select(x => x.Value)
                .FirstOrDefault(x =>
                    x.TrimStart().StartsWith("{"));

            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }
        catch
        {
            // La réponse peut déjà être du JSON.
        }

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');

        if (start >= 0 && end > start)
        {
            return response.Substring(
                start,
                end - start + 1);
        }

        throw new Exception(
            "Impossible d'extraire le JSON de la réponse AIKA.");
    }

    private static string GetString(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
            return "";

        return value.ToString();
    }

    private static int GetInt(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
            return 0;

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var number))
            return number;

        return int.TryParse(
            value.ToString(),
            out var result)
                ? result
                : 0;
    }

    private static double GetDouble(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(
                property,
                out var value))
            return 0;

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetDouble(out var number))
            return number;

        return double.TryParse(
            value.ToString(),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result)
                ? result
                : 0;
    }
}

public class AikaLocationResult
{
    public int DeviceId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Speed { get; set; }

    public double Course { get; set; }

    public bool IsGps { get; set; }

    public bool IsStopped { get; set; }

    public int Battery { get; set; }

    public string Status { get; set; } = "";

    public string PositionTime { get; set; } = "";
}