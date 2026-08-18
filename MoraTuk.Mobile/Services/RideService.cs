using System.Net.Http.Json;
using System.Text.Json;
using MoraTuk.Mobile.Helpers;
using MoraTuk.Mobile.Models;

namespace MoraTuk.Mobile.Services;

public class RideService
{
    private readonly HttpClient _http;

    public RideService()
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
    // CRÉER UNE COURSE
    // ============================================================

    public async Task<RideResponse?> CreateRideAsync(
        CreateRideDto dto)
    {
        try
        {
            if (dto == null)
            {
                throw new Exception(
                    "CreateRideDto est NULL.");
            }

            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            var url =
                $"{BaseUrl}/api/Ride/create";

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                $"CREATE RIDE URL : {url}");

            Console.WriteLine(
                $"ClientId : {dto.ClientId}");

            Console.WriteLine(
                $"Pickup : " +
                $"{dto.PickupLatitude}, " +
                $"{dto.PickupLongitude}");

            Console.WriteLine(
                $"Destination : " +
                $"{dto.DestinationLatitude}, " +
                $"{dto.DestinationLongitude}");

            Console.WriteLine(
                $"RideType : {dto.RideType}");

            Console.WriteLine(
                "====================================");

            var response =
                await _http.PostAsJsonAsync(
                    url,
                    dto);

            var responseBody =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"CREATE RIDE STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"CREATE RIDE RESPONSE : " +
                responseBody);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Erreur création course\n\n" +
                    $"HTTP : {(int)response.StatusCode}\n" +
                    $"Message : {responseBody}");
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new Exception(
                    "L'API a retourné une réponse vide.");
            }

            var result =
                JsonSerializer.Deserialize<RideResponse>(
                    responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                throw new Exception(
                    "Impossible de convertir la réponse " +
                    "en RideResponse.");
            }

            Console.WriteLine(
                $"COURSE CRÉÉE : {result.Id}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR CreateRideAsync : {ex}");

            throw;
        }
    }


    // ============================================================
    // COURSES ACTIVES DU CHAUFFEUR
    // ============================================================

    public async Task<List<RideNotification>>
        GetAvailableRidesAsync(int driverId)
    {
        try
        {
            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            var url =
                $"{BaseUrl}/api/Ride/available/{driverId}";

            Console.WriteLine(
                $"ACTIVE RIDES URL : {url}");

            var response =
                await _http.GetAsync(url);

            var content =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"ACTIVE RIDES STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"ACTIVE RIDES RESPONSE : " +
                content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Erreur API {(int)response.StatusCode} : " +
                    content);
            }

            var rides =
                JsonSerializer.Deserialize<
                    List<RideNotification>>(
                        content,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

            return rides ??
                new List<RideNotification>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR GetAvailableRidesAsync : {ex}");

            throw;
        }
    }

    public class AcceptRideResult
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string Message { get; set; } = "";

        public string ResponseBody { get; set; } = "";
    }

    public class RejectRideResult
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string Message { get; set; } = "";

        public string ResponseBody { get; set; } = "";
    }

    public class PaymentStatusResult
    {
        public bool Success { get; set; }

        public bool Confirmed { get; set; }

        public bool Pending { get; set; }

        public bool Failed { get; set; }

        public int RideId { get; set; }

        public int PaymentId { get; set; }

        public string? RideStatus { get; set; }

        public string? PaymentStatus { get; set; }

        public string? MvolaStatus { get; set; }

        public string? ServerCorrelationId { get; set; }

        public string? Result { get; set; }

        public string? Message { get; set; }
    }



    // ============================================================
    // ACCEPTER COURSE
    // ============================================================

    public async Task<AcceptRideResult> AcceptRideAsync(
    int rideId,
    int driverId)
        {
            try
            {
                if (!ApiSettings.IsConfigured)
                {
                    return new AcceptRideResult
                    {
                        Success = false,
                        Message =
                            "ApiSettings.BaseUrl n'est pas configuré."
                    };
                }

                var url =
                    $"{BaseUrl}/api/Ride/{rideId}" +
                    $"/accept?driverId={driverId}";

                Console.WriteLine();
                Console.WriteLine("====================================");
                Console.WriteLine("ACCEPTATION COURSE MOBILE");
                Console.WriteLine($"RideId   : {rideId}");
                Console.WriteLine($"DriverId : {driverId}");
                Console.WriteLine($"URL      : {url}");
                Console.WriteLine("====================================");

                var response =
                    await _http.PutAsync(
                        url,
                        null);

                var content =
                    await response.Content
                        .ReadAsStringAsync();

                Console.WriteLine();
                Console.WriteLine("========== ACCEPT RESPONSE ==========");
                Console.WriteLine(
                    $"HTTP : {(int)response.StatusCode}");

                Console.WriteLine(
                    $"BODY : {content}");

                Console.WriteLine(
                    "====================================");

                return new AcceptRideResult
                {
                    Success = response.IsSuccessStatusCode,

                    StatusCode =
                        (int)response.StatusCode,

                    ResponseBody =
                        content,

                    Message =
                        response.IsSuccessStatusCode
                            ? "Course acceptée."
                            : $"Erreur HTTP {(int)response.StatusCode} - URL : {url}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========== ACCEPT EXCEPTION ==========");

                Console.WriteLine(ex.ToString());

                Console.WriteLine(
                    "======================================");

                return new AcceptRideResult
                {
                    Success = false,

                    StatusCode = 0,

                    Message = ex.Message,

                    ResponseBody = ex.ToString()
                };
            }
        }
    // ============================================================
// REFUSER COURSE
// ============================================================

// ============================================================
// REFUSER UNE COURSE
// ============================================================

    public async Task<RejectRideResult> RejectRideAsync(
        int rideId,
        int driverId)
    {
        try
        {
            if (!ApiSettings.IsConfigured)
            {
                return new RejectRideResult
                {
                    Success = false,
                    StatusCode = 0,
                    Message = "ApiSettings.BaseUrl n'est pas configuré."
                };
            }

            var url =
                $"{BaseUrl}/api/Ride/{rideId}/reject?driverId={driverId}";

            Console.WriteLine();
            Console.WriteLine("====================================");
            Console.WriteLine("REFUS COURSE MOBILE");
            Console.WriteLine($"RideId   : {rideId}");
            Console.WriteLine($"DriverId : {driverId}");
            Console.WriteLine($"URL      : {url}");
            Console.WriteLine("====================================");

            var response = await _http.PutAsync(url, null);

            var content =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"REJECT STATUS : {(int)response.StatusCode}");

            Console.WriteLine(
                $"REJECT RESPONSE : {content}");

            return new RejectRideResult
            {
                Success = response.IsSuccessStatusCode,

                StatusCode =
                    (int)response.StatusCode,

                Message =
                    response.IsSuccessStatusCode
                        ? "Course refusée."
                        : $"Erreur HTTP {(int)response.StatusCode}",

                ResponseBody = content
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "========== REJECT EXCEPTION ==========");

            Console.WriteLine(ex.ToString());

            return new RejectRideResult
            {
                Success = false,
                StatusCode = 0,
                Message = ex.Message,
                ResponseBody = ex.ToString()
            };
        }
    }

    // ============================================================
    // TERMINER COURSE
    // ============================================================

    public async Task<bool> CompleteRideAsync(
        int rideId,
        int driverId)
    {
        try
        {
            var url =
                $"{BaseUrl}/api/Ride/{rideId}" +
                $"/complete?driverId={driverId}";

            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                $"COMPLETE RIDE URL : {url}");

            Console.WriteLine(
                $"RideId   : {rideId}");

            Console.WriteLine(
                $"DriverId : {driverId}");

            Console.WriteLine(
                "====================================");

            var response =
                await _http.PutAsync(
                    url,
                    null);

            var content =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"COMPLETE RIDE STATUS : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"COMPLETE RIDE RESPONSE : " +
                content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"COMPLETE RIDE ERROR : {content}");

                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR CompleteRideAsync : {ex}");

            return false;
        }
    }
    // ============================================================
    // VERIFIER STATUT PAIEMENT MVOLA
    // ============================================================

    public async Task<PaymentStatusResult?> GetPaymentStatusAsync(
        int rideId)
    {
        try
        {
            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            var url =
                $"{BaseUrl}/api/Ride/{rideId}/payment-status";

            Console.WriteLine();
            Console.WriteLine("====================================");
            Console.WriteLine("VERIFICATION PAIEMENT MVOLA");
            Console.WriteLine($"RideId : {rideId}");
            Console.WriteLine($"URL    : {url}");
            Console.WriteLine("====================================");

            var response =
                await _http.GetAsync(url);

            var content =
                await response.Content
                    .ReadAsStringAsync();

            Console.WriteLine(
                $"PAYMENT STATUS HTTP : " +
                $"{(int)response.StatusCode}");

            Console.WriteLine(
                $"PAYMENT STATUS BODY : " +
                content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"ERREUR PAYMENT STATUS : {content}");

                return new PaymentStatusResult
                {
                    Success = false,
                    Message = content
                };
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new PaymentStatusResult
                {
                    Success = false,
                    Message =
                        "Réponse paiement vide."
                };
            }

            var result =
                JsonSerializer.Deserialize<PaymentStatusResult>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR GetPaymentStatusAsync : {ex}");

            return new PaymentStatusResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
    // ============================================================
    // RECUPERER UNE COURSE
    // ============================================================

    public async Task<RideResponse?> GetRideAsync(int rideId)
    {
        try
        {
            if (!ApiSettings.IsConfigured)
            {
                throw new Exception(
                    "ApiSettings.BaseUrl n'est pas configuré.");
            }

            var url =
                $"{BaseUrl}/api/Ride/{rideId}";

            Console.WriteLine();
            Console.WriteLine("====================================");
            Console.WriteLine("RECUPERATION COURSE");
            Console.WriteLine($"RideId : {rideId}");
            Console.WriteLine($"URL    : {url}");
            Console.WriteLine("====================================");

            var response =
                await _http.GetAsync(url);

            var content =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"GET RIDE STATUS : {(int)response.StatusCode}");

            Console.WriteLine(
                $"GET RIDE RESPONSE : {content}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"ERREUR GET RIDE : {content}");

                return null;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            var result =
                JsonSerializer.Deserialize<RideResponse>(
                    content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR GetRideAsync : {ex}");

            return null;
        }
    }
}