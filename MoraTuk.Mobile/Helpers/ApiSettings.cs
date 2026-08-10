namespace MoraTuk.Mobile.Helpers;

public static class ApiSettings
{
    private static string _baseUrl = string.Empty;

    public static string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value?.TrimEnd('/') ?? string.Empty;
    }

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_baseUrl);
}