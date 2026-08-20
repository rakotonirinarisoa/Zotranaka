using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace MoraTuk.API.Services;

public class AikaLocationService
{
    private readonly HttpClient _httpClient;

    private const string BaseUrl =
        "http://app.aika168.com:8088/openapiv3.asmx";

    private const string AppKey =
        "7DU2DJFDR8321";

    // ============================================================
    // IMPORTANT
    //
    // LOGIN AIKA :
    //
    // Le Login n'est PAS exécuté à chaque GetTracking.
    //
    // Une fois le Login réussi :
    //
    // Username
    //      ↓
    // DeviceID
    // Model
    // Key
    //      ↓
    // CACHE
    //
    // Tous les GetTracking suivants utilisent directement
    // les informations du cache.
    // ============================================================

    private static readonly ConcurrentDictionary<
        string,
        AikaDeviceInfo
    > DeviceCache = new();

    // ============================================================
    // ECHECS LOGIN
    //
    // Exemple :
    //
    // AIKA -> state 2001
    //
    // On bloque les nouvelles tentatives pendant 10 minutes.
    // ============================================================

    private static readonly TimeSpan LoginRetryDelay =
        TimeSpan.FromMinutes(10);

    private static readonly ConcurrentDictionary<
        string,
        AikaLoginFailure
    > LoginFailureCache = new();

    // ============================================================
    // LOCK LOGIN
    //
    // Empêche plusieurs Login simultanés pour le même compte.
    // ============================================================

    private static readonly ConcurrentDictionary<
        string,
        SemaphoreSlim
    > LoginLocks = new();

    public AikaLocationService(
        HttpClient httpClient)
    {
        _httpClient = httpClient;

        // Timeout raisonnable
        _httpClient.Timeout =
            TimeSpan.FromSeconds(30);
    }

    // ============================================================
    // LOGIN
    //
    // CETTE METHODE N'EST APPELEE QUE SI LE CACHE EST ABSENT.
    // ============================================================

    public async Task<AikaDeviceInfo> LoginAndGetDeviceInfoAsync(
        string username,
        string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException(
                "Username AIKA vide.",
                nameof(username));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException(
                "Password AIKA vide.",
                nameof(password));

        var cacheKey =
            username.Trim();

        // ========================================================
        // 1. CACHE
        // ========================================================

        if (DeviceCache.TryGetValue(
                cacheKey,
                out var cachedDevice))
        {
            Console.WriteLine(
                $"AIKA LOGIN NON NECESSAIRE : " +
                $"cache trouvé pour {username}, " +
                $"DeviceID={cachedDevice.DeviceId}");

            return cachedDevice;
        }

        // ========================================================
        // 2. ECHEC LOGIN RECENT
        // ========================================================

        if (LoginFailureCache.TryGetValue(
                cacheKey,
                out var failure))
        {
            if (DateTime.UtcNow <
                failure.RetryAfterUtc)
            {
                var remaining =
                    failure.RetryAfterUtc -
                    DateTime.UtcNow;

                throw new AikaLoginTemporarilyBlockedException(
                    username,
                    failure.LastState,
                    failure.RetryAfterUtc);
            }

            LoginFailureCache.TryRemove(
                cacheKey,
                out _);
        }

        // ========================================================
        // 3. LOCK
        // ========================================================

        var loginLock =
            LoginLocks.GetOrAdd(
                cacheKey,
                _ => new SemaphoreSlim(1, 1));

        await loginLock.WaitAsync();

        try
        {
            // ====================================================
            // DOUBLE CHECK CACHE
            // ====================================================

            if (DeviceCache.TryGetValue(
                    cacheKey,
                    out cachedDevice))
            {
                Console.WriteLine(
                    $"AIKA LOGIN NON NECESSAIRE : " +
                    $"cache trouvé après lock pour {username}");

                return cachedDevice;
            }

            // ====================================================
            // DOUBLE CHECK FAILURE
            // ====================================================

            if (LoginFailureCache.TryGetValue(
                    cacheKey,
                    out failure))
            {
                if (DateTime.UtcNow <
                    failure.RetryAfterUtc)
                {
                    throw new AikaLoginTemporarilyBlockedException(
                        username,
                        failure.LastState,
                        failure.RetryAfterUtc);
                }

                LoginFailureCache.TryRemove(
                    cacheKey,
                    out _);
            }

            // ====================================================
            // MAINTENANT SEULEMENT : LOGIN
            // ====================================================

            Console.WriteLine(
                "============================================");

            Console.WriteLine(
                "AIKA LOGIN EXECUTE");

            Console.WriteLine(
                $"Username : {username}");

            Console.WriteLine(
                "============================================");

            var payload =
                new Dictionary<string, string>
                {
                    ["Name"] =
                        username,

                    ["Pass"] =
                        password,

                    ["LoginType"] =
                        "1",

                    ["LoginAPP"] =
                        "AKSH",

                    ["GMT"] =
                        "2:00",

                    ["Key"] =
                        AppKey
                };

            using var content =
                new FormUrlEncodedContent(payload);

            HttpResponseMessage response;

            try
            {
                response =
                    await _httpClient.PostAsync(
                        $"{BaseUrl}/Login",
                        content);
            }
            catch (Exception ex)
            {
                RegisterLoginFailure(
                    cacheKey,
                    "HTTP_EXCEPTION");

                Console.WriteLine(
                    $"AIKA LOGIN HTTP ERROR : {ex.Message}");

                throw;
            }

            var xml =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"AIKA LOGIN HTTP = {(int)response.StatusCode}");

            Console.WriteLine(
                $"AIKA LOGIN RESPONSE = {xml}");

            if (!response.IsSuccessStatusCode)
            {
                RegisterLoginFailure(
                    cacheKey,
                    $"HTTP_{(int)response.StatusCode}");

                throw new Exception(
                    $"AIKA Login HTTP {(int)response.StatusCode}: {xml}");
            }

            // ====================================================
            // XML -> JSON
            // ====================================================

            var jsonText =
                ExtractJsonFromXml(xml);

            using var document =
                JsonDocument.Parse(jsonText);

            var root =
                document.RootElement;

            var state =
                GetString(
                    root,
                    "state");

            // ====================================================
            // LOGIN ECHEC
            // ====================================================

            if (state != "0")
            {
                RegisterLoginFailure(
                    cacheKey,
                    state);

                Console.WriteLine(
                    $"AIKA LOGIN ECHEC : State={state}");

                throw new AikaLoginFailedException(
                    username,
                    state);
            }

            // ====================================================
            // DEVICE INFO
            // ====================================================

            if (!root.TryGetProperty(
                    "deviceInfo",
                    out var deviceInfo))
            {
                RegisterLoginFailure(
                    cacheKey,
                    "DEVICE_INFO_MISSING");

                throw new Exception(
                    "AIKA Login : deviceInfo absent.");
            }

            var device =
                new AikaDeviceInfo
                {
                    DeviceId =
                        GetString(
                            deviceInfo,
                            "deviceID"),

                    DeviceName =
                        GetString(
                            deviceInfo,
                            "deviceName"),

                    Model =
                        GetString(
                            deviceInfo,
                            "model"),

                    SerialNumber =
                        GetString(
                            deviceInfo,
                            "sn"),

                    Imei =
                        GetString(
                            deviceInfo,
                            "ICCID"),

                    Key =
                        GetString(
                            deviceInfo,
                            "key2018")
                };

            // ====================================================
            // VALIDATION
            // ====================================================

            if (string.IsNullOrWhiteSpace(
                    device.DeviceId))
                throw new Exception(
                    "AIKA Login : DeviceID vide.");

            if (string.IsNullOrWhiteSpace(
                    device.Model))
                throw new Exception(
                    "AIKA Login : Model vide.");

            if (string.IsNullOrWhiteSpace(
                    device.Key))
                throw new Exception(
                    "AIKA Login : Key vide.");

            // ====================================================
            // CACHE
            //
            // A PARTIR DE MAINTENANT :
            //
            // GetTracking ne fera PLUS de Login.
            // ====================================================

            DeviceCache[
                cacheKey
            ] = device;

            LoginFailureCache.TryRemove(
                cacheKey,
                out _);

            Console.WriteLine(
                "============================================");

            Console.WriteLine(
                "AIKA LOGIN OK");

            Console.WriteLine(
                $"Username  : {username}");

            Console.WriteLine(
                $"DeviceID  : {device.DeviceId}");

            Console.WriteLine(
                $"DeviceName: {device.DeviceName}");

            Console.WriteLine(
                $"Model     : {device.Model}");

            Console.WriteLine(
                $"Serial    : {device.SerialNumber}");

            Console.WriteLine(
                "============================================");

            return device;
        }
        finally
        {
            loginLock.Release();
        }
    }

    // ============================================================
    // GET TRACKING
    //
    // IMPORTANT :
    //
    // Cette méthode NE FAIT PAS DE LOGIN.
    //
    // Si le device est déjà en cache :
    //
    //     GetTracking
    //     GetTracking
    //     GetTracking
    //     GetTracking
    //
    // sans Login.
    // ============================================================

    public async Task<AikaLocation?> GetTrackingAsync(
        int deviceId,
        string username,
        string password)
    {
        var cacheKey =
            username.Trim();

        // ========================================================
        // CACHE
        // ========================================================

        if (!DeviceCache.TryGetValue(
                cacheKey,
                out var device))
        {
            // ====================================================
            // PREMIER APPEL UNIQUEMENT
            //
            // Le Login est nécessaire seulement ici.
            // ====================================================

            Console.WriteLine(
                $"AIKA : aucun device en cache pour {username}");

            device =
                await LoginAndGetDeviceInfoAsync(
                    username,
                    password);
        }
        else
        {
            Console.WriteLine(
                $"AIKA TRACKING : utilisation cache " +
                $"DeviceID={device.DeviceId}");
        }

        // ========================================================
        // SECURITE
        // ========================================================

        if (string.IsNullOrWhiteSpace(
                device.DeviceId))
            throw new Exception(
                "AIKA : DeviceID vide.");

        if (string.IsNullOrWhiteSpace(
                device.Model))
            throw new Exception(
                "AIKA : Model vide.");

        if (string.IsNullOrWhiteSpace(
                device.Key))
            throw new Exception(
                "AIKA : Key vide.");

        // ========================================================
        // GET TRACKING
        // ========================================================

        var payload =
            new Dictionary<string, string>
            {
                ["DeviceID"] =
                    device.DeviceId,

                ["Model"] =
                    device.Model,

                ["TimeZones"] =
                    "2:00",

                ["MapType"] =
                    "Google",

                ["Language"] =
                    "en",

                ["Key"] =
                    device.Key,

                ["type"] =
                    "1"
            };

        using var content =
            new FormUrlEncodedContent(payload);

        Console.WriteLine(
            "--------------------------------------------");

        Console.WriteLine(
            "AIKA GET TRACKING");

        Console.WriteLine(
            $"Username : {username}");

        Console.WriteLine(
            $"DeviceID : {device.DeviceId}");

        Console.WriteLine(
            $"Model    : {device.Model}");

        Console.WriteLine(
            "--------------------------------------------");

        var response =
            await _httpClient.PostAsync(
                $"{BaseUrl}/GetTracking",
                content);

        var xml =
            await response.Content
                .ReadAsStringAsync();

        Console.WriteLine(
            $"AIKA GET TRACKING HTTP = {(int)response.StatusCode}");

        Console.WriteLine(
            $"AIKA GET TRACKING RESPONSE = {xml}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"AIKA GetTracking HTTP {(int)response.StatusCode}: {xml}");
        }

        // ========================================================
        // XML -> JSON
        // ========================================================

        var jsonText =
            ExtractJsonFromXml(xml);

        using var document =
            JsonDocument.Parse(jsonText);

        var root =
            document.RootElement;

        var state =
            GetString(
                root,
                "state");

        if (state != "0")
        {
            throw new Exception(
                $"AIKA GetTracking échoué. State={state}");
        }

        // ========================================================
        // LAT / LNG
        // ========================================================

        var latitude =
            GetDouble(
                root,
                "lat");

        var longitude =
            GetDouble(
                root,
                "lng");

        var speed =
            GetDouble(
                root,
                "speed");

        // Vitesse AIKA en km/h
        var speedKmh =
            Math.Round(speed, 1);

        var course =
            GetDouble(
                root,
                "course");

        var isGps =
            GetInt(
                root,
                "isGPS") == 1;

        var isStopped =
            GetInt(
                root,
                "isStop") == 1;

        var battery =
            GetInt(
                root,
                "battery");

        var positionTime =
            GetString(
                root,
                "positionTime");

        var status =
            GetString(
                root,
                "status");

        Console.WriteLine(
            "============================================");

        Console.WriteLine(
            "AIKA POSITION");

        Console.WriteLine(
            $"DeviceID  : {device.DeviceId}");

        Console.WriteLine(
            $"LATITUDE  : {latitude}");

        Console.WriteLine(
            $"LONGITUDE : {longitude}");

        Console.WriteLine(
            $"Speed     : {speedKmh} km/h");

        Console.WriteLine(
            $"Course    : {course}");

        Console.WriteLine(
            $"GPS       : {isGps}");

        Console.WriteLine(
            $"Stopped   : {isStopped}");

        Console.WriteLine(
            $"Battery   : {battery}");

        Console.WriteLine(
            $"Position  : {positionTime}");

        Console.WriteLine(
            "============================================");

        // ========================================================
        // GPS INVALIDE
        // ========================================================

        if (!isGps)
            return null;

        if (latitude == 0 ||
            longitude == 0)
            return null;

        // ========================================================
        // RESULTAT
        // ========================================================

        return new AikaLocation
        {
            DeviceId =
                device.DeviceId,

            Latitude =
                latitude,

            Longitude =
                longitude,

           Speed =
                speedKmh,
            Course =
                course,

            PositionTime =
                positionTime,

            IsGps =
                isGps,

            IsStopped =
                isStopped,

            Battery =
                battery,

            Status =
                status
        };
    }

    // ============================================================
    // XML -> JSON
    // ============================================================

    private static string ExtractJsonFromXml(
        string xml)
    {
        var document =
            XDocument.Parse(xml);

        var value =
            document.Root?.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new Exception(
                "Réponse AIKA XML vide.");

        return value.Trim();
    }

    // ============================================================
    // STRING
    // ============================================================

    private static string GetString(
        JsonElement root,
        string property)
    {
        if (!root.TryGetProperty(
                property,
                out var element))
            return "";

        return element.ValueKind switch
        {
            JsonValueKind.String =>
                element.GetString() ?? "",

            JsonValueKind.Number =>
                element.ToString(),

            _ =>
                element.ToString()
        };
    }

    // ============================================================
    // DOUBLE
    // ============================================================

    private static double GetDouble(
        JsonElement root,
        string property)
    {
        var value =
            GetString(
                root,
                property);

        if (double.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var result))
        {
            return result;
        }

        return 0;
    }

    // ============================================================
    // INT
    // ============================================================

    private static int GetInt(
        JsonElement root,
        string property)
    {
        var value =
            GetString(
                root,
                property);

        if (int.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var result))
        {
            return result;
        }

        return 0;
    }

    // ============================================================
    // OPTIONNEL :
    // INVALIDER LE CACHE MANUELLEMENT
    //
    // On pourra utiliser cette méthode si AIKA indique un jour
    // que la session/device n'est plus valide.
    // ============================================================

    public static void ClearDeviceCache(
        string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;

        DeviceCache.TryRemove(
            username.Trim(),
            out _);

        Console.WriteLine(
            $"AIKA CACHE SUPPRIME : {username}");
    }

    private static void RegisterLoginFailure(
        string cacheKey,
        string state)
    {
        var retryAfter =
            DateTime.UtcNow.Add(
                LoginRetryDelay);

        var failureCount =
            LoginFailureCache.TryGetValue(
                cacheKey,
                out var previous)
                    ? previous.FailureCount + 1
                    : 1;

        LoginFailureCache[
            cacheKey
        ] = new AikaLoginFailure
        {
            LastState =
                state,

            FailureCount =
                failureCount,

            RetryAfterUtc =
                retryAfter
        };

        Console.WriteLine(
            $"AIKA LOGIN FAILURE CACHE : " +
            $"{cacheKey} / State={state} / " +
            $"RetryAfter={retryAfter:yyyy-MM-dd HH:mm:ss} UTC");
    }
}

// ================================================================
// LOGIN FAILURE
// ================================================================

public class AikaLoginFailure
{
    public DateTime RetryAfterUtc { get; set; }

    public int FailureCount { get; set; }

    public string LastState { get; set; } = "";
}

// ================================================================
// EXCEPTION LOGIN BLOQUE
// ================================================================

public class AikaLoginTemporarilyBlockedException
    : Exception
{
    public string Username { get; }

    public string State { get; }

    public DateTime RetryAfterUtc { get; }

    public AikaLoginTemporarilyBlockedException(
        string username,
        string state,
        DateTime retryAfterUtc)
        : base(
            $"Login AIKA temporairement bloqué pour {username}. " +
            $"State={state}. " +
            $"Nouvelle tentative après {retryAfterUtc:yyyy-MM-dd HH:mm:ss} UTC.")
    {
        Username =
            username;

        State =
            state;

        RetryAfterUtc =
            retryAfterUtc;
    }
}

// ================================================================
// EXCEPTION LOGIN
// ================================================================

public class AikaLoginFailedException
    : Exception
{
    public string Username { get; }

    public string State { get; }

    public AikaLoginFailedException(
        string username,
        string state)
        : base(
            $"AIKA Login échoué pour {username}. State={state}.")
    {
        Username =
            username;

        State =
            state;
    }
}

// ================================================================
// DEVICE
// ================================================================

public class AikaDeviceInfo
{
    public string DeviceId { get; set; } = "";

    public string DeviceName { get; set; } = "";

    public string Model { get; set; } = "";

    public string SerialNumber { get; set; } = "";

    public string Imei { get; set; } = "";

    public string Key { get; set; } = "";
}

// ================================================================
// LOCATION
// ================================================================

public class AikaLocation
{
    public string DeviceId { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Speed { get; set; }

    public double Course { get; set; }

    public string PositionTime { get; set; } = "";

    public bool IsGps { get; set; }

    public bool IsStopped { get; set; }

    public int Battery { get; set; }

    public string Status { get; set; } = "";
}