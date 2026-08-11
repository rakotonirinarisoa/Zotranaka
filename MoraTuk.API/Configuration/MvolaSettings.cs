namespace MoraTuk.API.Configuration;

public class MvolaSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
    public string ConsumerKey { get; set; } = string.Empty;
    public string ConsumerSecret { get; set; } = string.Empty;

    public string MerchantNumber { get; set; } = string.Empty;
    public string PartnerName { get; set; } = "MoraTUK";
}