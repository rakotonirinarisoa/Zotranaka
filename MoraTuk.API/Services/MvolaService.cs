using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MoraTuk.API.Models;

namespace MoraTuk.API.Services
{
    public class MvolaService : IMvolaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MvolaService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // ============================================================
        // TOKEN MVOLA
        // ============================================================

        private async Task<string> GetAccessTokenAsync()
        {
            var mvolaSection =
                _configuration.GetSection("Mvola");

            var tokenUrl = mvolaSection["TokenUrl"];
            var consumerKey = mvolaSection["ConsumerKey"];
            var consumerSecret = mvolaSection["ConsumerSecret"];

            if (string.IsNullOrWhiteSpace(tokenUrl))
                throw new Exception("Mvola:TokenUrl est manquant.");

            if (string.IsNullOrWhiteSpace(consumerKey))
                throw new Exception("Mvola:ConsumerKey est manquant.");

            if (string.IsNullOrWhiteSpace(consumerSecret))
                throw new Exception("Mvola:ConsumerSecret est manquant.");

            var credentials =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{consumerKey}:{consumerSecret}"));

            using var tokenRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    tokenUrl);

            tokenRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    credentials);

            tokenRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            tokenRequest.Content =
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["scope"] = "EXT_INT_MVOLA_SCOPE"
                    });

            var tokenResponse =
                await _httpClient.SendAsync(tokenRequest);

            var tokenContent =
                await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"MVola OAuth HTTP {(int)tokenResponse.StatusCode}: {tokenContent}");
            }

            using var tokenJson =
                JsonDocument.Parse(tokenContent);

            if (!tokenJson.RootElement.TryGetProperty(
                    "access_token",
                    out var accessTokenProperty))
            {
                throw new Exception(
                    $"Token MVola invalide : {tokenContent}");
            }

            var accessToken =
                accessTokenProperty.GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception(
                    "MVola : access_token vide.");

            return accessToken;
        }

        // ============================================================
        // CONFIGURATION COMMUNE
        // ============================================================

        private string GetBaseUrl()
        {
            var baseUrl =
                _configuration["Mvola:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new Exception(
                    "Mvola:BaseUrl est manquant.");

            return baseUrl.TrimEnd('/');
        }

        private string GetMerchantNumber()
        {
            var merchantNumber =
                _configuration["Mvola:MerchantNumber"];

            if (string.IsNullOrWhiteSpace(merchantNumber))
                throw new Exception(
                    "Mvola:MerchantNumber est manquant.");

            // On évite un éventuel préfixe déjà présent
            merchantNumber =
                merchantNumber.Trim();

            if (merchantNumber.StartsWith("msisdn;"))
            {
                merchantNumber =
                    merchantNumber.Substring("msisdn;".Length);
            }

            return merchantNumber;
        }

        private string GetUserAccountIdentifier()
        {
            var merchantNumber =
                GetMerchantNumber();

            // FORMAT MVOLA :
            // msisdn;0343500004
            return $"msisdn;{merchantNumber}";
        }

        private string GetPartnerName()
        {
            return _configuration["Mvola:PartnerName"]
                   ?? "MoraTUK";
        }

        // ============================================================
        // POST MERCHANT PAY
        // ============================================================

        public async Task<string> MerchantPayAsync(
            MvolaPaymentRequest request)
        {
            var accessToken =
                await GetAccessTokenAsync();

            var baseUrl =
                GetBaseUrl();

            var userAccountIdentifier =
                GetUserAccountIdentifier();

            var partnerName =
                GetPartnerName();

            // ========================================================
            // SERIALISATION
            // ========================================================

            var jsonOptions =
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase,

                    DefaultIgnoreCondition =
                        System.Text.Json.Serialization
                            .JsonIgnoreCondition.WhenWritingNull
                };

            var mvolaRequest = new
            {
                request.Amount,
                request.Currency,
                request.DescriptionText,
                request.RequestingOrganisationTransactionReference,
                request.RequestDate,
                request.OriginalTransactionReference,
                request.DebitParty,
                request.CreditParty,
                request.Metadata
            };

            var json =
                JsonSerializer.Serialize(
                    mvolaRequest,
                    jsonOptions);

            Console.WriteLine();
            Console.WriteLine(
                "========== MVOLA REQUEST ==========");
            Console.WriteLine(json);
            Console.WriteLine(
                "===================================");

            // ========================================================
            // URL
            // ========================================================

            var merchantPayUrl =
                $"{baseUrl}/";

            var correlationId =
                Guid.NewGuid().ToString();

            using var merchantRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    merchantPayUrl);

            // ========================================================
            // HEADERS
            // ========================================================

            merchantRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            merchantRequest.Headers.Add(
                "Version",
                "1.0");

            merchantRequest.Headers.Add(
                "X-CorrelationID",
                correlationId);

            merchantRequest.Headers.Add(
                "UserLanguage",
                "FR");

            merchantRequest.Headers.Add(
                "UserAccountIdentifier",
                userAccountIdentifier);

            merchantRequest.Headers.Add(
                "partnerName",
                partnerName);

            merchantRequest.Headers.Add(
                "Cache-Control",
                "no-cache");

            merchantRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            merchantRequest.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            // ========================================================
            // DEBUG
            // ========================================================

            Console.WriteLine();
            Console.WriteLine(
                "========== MVOLA REQUEST DEBUG ==========");

            Console.WriteLine(
                $"URL                  : {merchantPayUrl}");

            Console.WriteLine(
                $"Version              : 1.0");

            Console.WriteLine(
                $"X-CorrelationID      : {correlationId}");

            Console.WriteLine(
                $"UserAccountIdentifier: {userAccountIdentifier}");

            Console.WriteLine(
                $"partnerName          : {partnerName}");

            Console.WriteLine(
                "BODY:");

            Console.WriteLine(json);

            Console.WriteLine(
                "=========================================");

            // ========================================================
            // APPEL MVOLA
            // ========================================================

            var response =
                await _httpClient.SendAsync(
                    merchantRequest);

            var responseContent =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine();
            Console.WriteLine(
                "========== MVOLA RESPONSE ==========");

            Console.WriteLine(
                $"HTTP {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(responseContent);

            Console.WriteLine(
                "====================================");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"MVola Merchant Pay HTTP {(int)response.StatusCode}: {responseContent}");
            }

            return responseContent;
        }

        // ============================================================
        // GET STATUS
        // GET /status/{serverCorrelationId}
        // ============================================================

        public async Task<string> GetPaymentStatusAsync(
            string serverCorrelationId)
        {
            if (string.IsNullOrWhiteSpace(
                serverCorrelationId))
            {
                throw new Exception(
                    "serverCorrelationId est obligatoire.");
            }

            var accessToken =
                await GetAccessTokenAsync();

            var baseUrl =
                GetBaseUrl();

            var userAccountIdentifier =
                GetUserAccountIdentifier();

            var partnerName =
                GetPartnerName();

            var correlationId =
                Guid.NewGuid().ToString();

            var statusUrl =
                $"{baseUrl}/status/{serverCorrelationId}";

            using var statusRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    statusUrl);

            // ========================================================
            // HEADERS
            // ========================================================

            statusRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            statusRequest.Headers.Add(
                "Version",
                "1.0");

            statusRequest.Headers.Add(
                "X-CorrelationID",
                correlationId);

            statusRequest.Headers.Add(
                "UserAccountIdentifier",
                userAccountIdentifier);

            statusRequest.Headers.Add(
                "partnerName",
                partnerName);

            statusRequest.Headers.Add(
                "Cache-Control",
                "no-cache");

            statusRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            // ========================================================
            // DEBUG
            // ========================================================

            Console.WriteLine();
            Console.WriteLine(
                "========== MVOLA STATUS REQUEST ==========");

            Console.WriteLine(
                $"URL                  : {statusUrl}");

            Console.WriteLine(
                $"Version              : 1.0");

            Console.WriteLine(
                $"X-CorrelationID      : {correlationId}");

            Console.WriteLine(
                $"ServerCorrelationId  : {serverCorrelationId}");

            Console.WriteLine(
                $"UserAccountIdentifier: {userAccountIdentifier}");

            Console.WriteLine(
                $"partnerName          : {partnerName}");

            Console.WriteLine(
                "==========================================");

            // ========================================================
            // APPEL MVOLA
            // ========================================================

            var response =
                await _httpClient.SendAsync(
                    statusRequest);

            var responseContent =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine();
            Console.WriteLine(
                "========== MVOLA STATUS RESPONSE ==========");

            Console.WriteLine(
                $"HTTP {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(responseContent);

            Console.WriteLine(
                "===========================================");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"MVola Status HTTP {(int)response.StatusCode}: {responseContent}");
            }

            return responseContent;
        }

        // ============================================================
        // GET TRANSACTION
        // GET /{transactionReference}
        // ============================================================

        public async Task<string> GetTransactionAsync(
            string transactionReference)
        {
            if (string.IsNullOrWhiteSpace(
                transactionReference))
            {
                throw new Exception(
                    "transactionReference est obligatoire.");
            }

            var accessToken =
                await GetAccessTokenAsync();

            var baseUrl =
                GetBaseUrl();

            var userAccountIdentifier =
                GetUserAccountIdentifier();

            var correlationId =
                Guid.NewGuid().ToString();

            var transactionUrl =
                $"{baseUrl}/{transactionReference}";

            using var transactionRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    transactionUrl);

            // ========================================================
            // HEADERS
            // ========================================================

            transactionRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            transactionRequest.Headers.Add(
                "Version",
                "1.0");

            transactionRequest.Headers.Add(
                "X-CorrelationID",
                correlationId);

            transactionRequest.Headers.Add(
                "UserAccountIdentifier",
                userAccountIdentifier);

            transactionRequest.Headers.Add(
                "Cache-Control",
                "no-cache");

            transactionRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            // ========================================================
            // DEBUG
            // ========================================================

            Console.WriteLine();
            Console.WriteLine(
                "========== MVOLA TRANSACTION REQUEST ==========");

            Console.WriteLine(
                $"URL                  : {transactionUrl}");

            Console.WriteLine(
                $"TransactionReference : {transactionReference}");

            Console.WriteLine(
                $"X-CorrelationID      : {correlationId}");

            Console.WriteLine(
                $"UserAccountIdentifier: {userAccountIdentifier}");

            Console.WriteLine(
                "===============================================");

            // ========================================================
            // APPEL MVOLA
            // ========================================================

            var response =
                await _httpClient.SendAsync(
                    transactionRequest);

            var responseContent =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine();
            Console.WriteLine(
                "========== MVOLA TRANSACTION RESPONSE ==========");

            Console.WriteLine(
                $"HTTP {(int)response.StatusCode} {response.StatusCode}");

            Console.WriteLine(responseContent);

            Console.WriteLine(
                "================================================");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"MVola Transaction HTTP {(int)response.StatusCode}: {responseContent}");
            }

            return responseContent;
        }
    }
}