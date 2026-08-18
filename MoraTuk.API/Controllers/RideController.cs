using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MoraTuk.API.Data;
using MoraTuk.API.Models;
using MoraTuk.API.Services;
using MoraTuk.API.Hubs;
using System.Text.Json;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RideController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DistanceService _distanceService;
    private readonly IHubContext<TrackingHub> _hub;
    private readonly IMvolaService _mvolaService;
    private readonly IConfiguration _configuration;

    private const decimal PRICE_PER_KM = 1500m;
    private const decimal MIN_RIDE_PRICE = 2100m;

    public RideController(
        AppDbContext context,
        DistanceService distanceService,
        IHubContext<TrackingHub> hub,
        IMvolaService mvolaService,
        IConfiguration configuration)
    {
        _context = context;
        _distanceService = distanceService;
        _hub = hub;
        _mvolaService = mvolaService;
        _configuration = configuration;
    }

    // ============================================================
    // ESTIMATION DU PRIX
    // ============================================================

    [HttpPost("estimate")]
    public IActionResult EstimatePrice([FromBody] Ride ride)
    {
        try
        {
            if (ride == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Les données de la course sont obligatoires."
                });
            }

            var distance = _distanceService.Calculate(
                ride.PickupLatitude,
                ride.PickupLongitude,
                ride.DestinationLatitude,
                ride.DestinationLongitude);

            if (distance <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La distance calculée est invalide."
                });
            }

            var price = CalculatePrice(
                distance,
                ride.RideType);

            return Ok(new
            {
                success = true,
                distanceKm = Math.Round(distance, 2),
                price
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERREUR ESTIMATION : {ex}");

            return StatusCode(500, new
            {
                success = false,
                message = "Erreur lors du calcul du prix.",
                error = ex.Message
            });
        }
    }

    // ============================================================
    // CREATION COURSE
    //
    // IMPORTANT :
    // Aucun appel MVola ici.
    // ============================================================

    [HttpPost("create")]
    public async Task<IActionResult> CreateRide(
        [FromBody] CreateRideRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Les données de la course sont obligatoires."
                });
            }

            // ========================================================
            // NUMERO MVOLA CLIENT
            // ========================================================

            if (string.IsNullOrWhiteSpace(request.DebitMsisdn))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le numéro MVola du client est obligatoire."
                });
            }

            var debitMsisdn =
                request.DebitMsisdn.Trim();

            // ========================================================
            // CLIENT
            // ========================================================

            var client = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Id == request.ClientId);

            if (client == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Client introuvable."
                });
            }

            if (!string.Equals(
                client.Role,
                "Client",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cet utilisateur n'est pas un client."
                });
            }

            // ========================================================
            // CHAUFFEURS DISPONIBLES
            // ========================================================

            var drivers = await _context.Drivers
                .Include(d => d.User)
                .Where(d => d.IsAvailable)
                .ToListAsync();

            Console.WriteLine(
                $"Nombre drivers disponibles : {drivers.Count}");

            if (!drivers.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Aucun chauffeur disponible."
                });
            }

            // ========================================================
            // DERNIERES POSITIONS
            // ========================================================

            var driverLocations = await _context.DriverLocations
                .GroupBy(x => x.DriverId)
                .Select(g => g
                    .OrderByDescending(x => x.CreatedAt)
                    .First())
                .ToListAsync();

            var nearestDriver = drivers
                .Join(
                    driverLocations,
                    driver => driver.Id,
                    location => location.DriverId,
                    (driver, location) => new
                    {
                        Driver = driver,

                        Distance = _distanceService.Calculate(
                            request.PickupLatitude,
                            request.PickupLongitude,
                            location.Latitude,
                            location.Longitude)
                    })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (nearestDriver == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Aucun chauffeur n'est sur une position connue."
                });
            }

            Console.WriteLine(
                $"Driver {nearestDriver.Driver.Id} - " +
                $"Distance : {nearestDriver.Distance:F2} km");

            if (nearestDriver.Distance > 5)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Aucun chauffeur disponible dans un rayon de 5 km."
                });
            }

            // ========================================================
            // DISTANCE COURSE
            // ========================================================

            var distance = _distanceService.Calculate(
                request.PickupLatitude,
                request.PickupLongitude,
                request.DestinationLatitude,
                request.DestinationLongitude);

            if (distance <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La distance calculée est invalide."
                });
            }

            // ========================================================
            // PRIX
            // ========================================================

            var price =
                CalculatePrice(
                    distance,
                    request.RideType);

            // ========================================================
            // MERCHANT NUMBER
            //
            // IMPORTANT :
            // Ceci est le numéro MoraTUK.
            //
            // Il ne faut JAMAIS utiliser ce numéro pour remplacer
            // DebitMsisdn.
            // ========================================================

            var merchantNumber =
                _configuration["Mvola:MerchantNumber"];

            if (string.IsNullOrWhiteSpace(merchantNumber))
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Mvola:MerchantNumber est manquant."
                });
            }

            merchantNumber =
                NormalizeMsisdn(
                    merchantNumber);

            Console.WriteLine(
                $"Merchant MVola : {merchantNumber}");

            Console.WriteLine(
                $"Client MVola   : {debitMsisdn}");

            // ========================================================
            // CREATION RIDE
            // ========================================================

            var ride = new Ride
            {
                ClientId =
                    request.ClientId,

                DriverId =
                    nearestDriver.Driver.Id,

                PickupLatitude =
                    request.PickupLatitude,

                PickupLongitude =
                    request.PickupLongitude,

                Departure =
                    request.Departure,

                DestinationLatitude =
                    request.DestinationLatitude,

                DestinationLongitude =
                    request.DestinationLongitude,

                Destination =
                    request.Destination,

                Price =
                    price,

                RideType =
                    string.IsNullOrWhiteSpace(
                        request.RideType)
                        ? "Shared"
                        : request.RideType,

                MaxPassengers = 4,

                CurrentPassengers = 1,

                Status =
                    "WaitingDriver",

                CreatedAt =
                    DateTime.UtcNow
            };

            _context.Rides.Add(ride);

            await _context.SaveChangesAsync();

            Console.WriteLine(
                $"Course créée : {ride.Id}");

            // ========================================================
            // CREATION PAYMENT
            //
            // Aucun appel MVola.
            // ========================================================

            var payment = new Payment
            {
                RideId =
                    ride.Id,

                Amount =
                    ride.Price,

                Currency =
                    "Ar",

                PaymentMethod =
                    "MVola",

                Status =
                    "Pending",

                // CLIENT
                DebitMsisdn =
                    debitMsisdn,

                // MORATUK
                CreditMsisdn =
                    merchantNumber,

                Description =
                    $"Paiement MoraTUK - Course #{ride.Id}",

                CreatedAt =
                    DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            Console.WriteLine(
                $"Payment créé : {payment.Id}");

            // ========================================================
            // NOTIFICATION CHAUFFEUR
            // ========================================================

            await _hub.Clients
                .Group($"driver-{nearestDriver.Driver.Id}")
                .SendAsync(
                    "NewRide",
                    new
                    {
                        RideId =
                            ride.Id,

                        DriverId =
                            nearestDriver.Driver.Id,

                        PickupLatitude =
                            ride.PickupLatitude,

                        PickupLongitude =
                            ride.PickupLongitude,

                        DestinationLatitude =
                            ride.DestinationLatitude,

                        DestinationLongitude =
                            ride.DestinationLongitude,

                        Price =
                            ride.Price,

                        RideType =
                            ride.RideType,

                        Passengers =
                            ride.CurrentPassengers,

                        DistanceToDriver =
                            nearestDriver.Distance,

                        Departure =
                            ride.Departure,

                        Destination =
                            ride.Destination,

                        Status =
                            ride.Status
                    });

            // ========================================================
            // RESPONSE
            // ========================================================

            return Ok(new
            {
                success = true,

                message =
                    "Course créée. En attente de l'acceptation du chauffeur.",

                rideId =
                    ride.Id,

                paymentId =
                    payment.Id,

                driverId =
                    nearestDriver.Driver.Id,

                driver =
                    nearestDriver.Driver.User?.FullName,

                distanceKm =
                    Math.Round(
                        nearestDriver.Distance,
                        2),

                rideDistanceKm =
                    Math.Round(
                        distance,
                        2),

                price =
                    ride.Price,

                rideType =
                    ride.RideType,

                passengers =
                    $"{ride.CurrentPassengers}/{ride.MaxPassengers}",

                rideStatus =
                    ride.Status,

                paymentStatus =
                    payment.Status,

                serverCorrelationId =
                    payment.ServerCorrelationId,

                transactionReference =
                    payment.TransactionReference
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR CREATE RIDE : {ex}");

            return StatusCode(500, new
            {
                success = false,

                message =
                    "Erreur lors de la création de la course.",

                error =
                    ex.Message,

                innerException =
                    ex.InnerException?.Message
            });
        }
    }

    // ============================================================
    // ACCEPTER COURSE
    //
    // MVola est lancé UNIQUEMENT ici.
    // ============================================================

    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptRide(
        int id,
        [FromQuery] int driverId)
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine(
                "==========================================");

            Console.WriteLine(
                $"ACCEPTATION COURSE #{id}");

            Console.WriteLine(
                $"DriverId : {driverId}");

            Console.WriteLine(
                "==========================================");

            // ========================================================
            // COURSE
            // ========================================================

            var ride = await _context.Rides
                .FirstOrDefaultAsync(
                    x => x.Id == id);

            if (ride == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Course introuvable."
                });
            }

            // ========================================================
            // STATUT
            // ========================================================

            if (ride.Status != "WaitingDriver")
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        $"Cette course n'est plus disponible. " +
                        $"Statut actuel : {ride.Status}"
                });
            }

            // ========================================================
            // CHAUFFEUR AFFECTE
            // ========================================================

            if (ride.DriverId != driverId)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Cette course n'est pas affectée à ce chauffeur."
                });
            }

            // ========================================================
            // VERIFIER COURSE ACTIVE
            // ========================================================

            var existingActiveRide =
                await _context.Rides
                    .FirstOrDefaultAsync(x =>
                        x.DriverId == driverId &&
                        (
                            x.Status == "Accepted" ||
                            x.Status == "InProgress"
                        ));

            if (existingActiveRide != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Ce chauffeur a déjà une course en cours.",
                    activeRideId =
                        existingActiveRide.Id,
                    activeRideStatus =
                        existingActiveRide.Status
                });
            }

            // ========================================================
            // CHAUFFEUR
            // ========================================================

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(
                    x => x.Id == driverId);

            if (driver == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Chauffeur introuvable."
                });
            }

            if (!driver.IsAvailable)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Ce chauffeur est déjà occupé."
                });
            }

            // ========================================================
            // PAYMENT
            // ========================================================

            var payment =
                await _context.Payments
                    .Where(x => x.RideId == ride.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

            if (payment == null)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Le paiement associé à cette course est introuvable."
                });
            }

            // ========================================================
            // CLIENT MVOLA
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                payment.DebitMsisdn))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le numéro MVola du client est manquant."
                });
            }

            var debitMsisdn =
                NormalizeMsisdn(
                    payment.DebitMsisdn);

            // ========================================================
            // MERCHANT MVOLA
            // ========================================================

            var merchantNumber =
                _configuration["Mvola:MerchantNumber"];

            if (string.IsNullOrWhiteSpace(
                merchantNumber))
            {
                return StatusCode(500, new
                {
                    success = false,
                    message =
                        "Mvola:MerchantNumber est manquant."
                });
            }

            merchantNumber =
                NormalizeMsisdn(
                    merchantNumber);

            // ========================================================
            // VERIFIER PAIEMENT
            // ========================================================

            if (payment.Status == "Success" ||
                payment.Status == "Completed")
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Le paiement de cette course est déjà confirmé.",
                    paymentStatus =
                        payment.Status
                });
            }

            // ========================================================
            // NOUVELLE TENTATIVE APRES ECHEC
            // ========================================================

            if (payment.Status == "Failed")
            {
                Console.WriteLine(
                    $"NOUVELLE TENTATIVE MVOLA - Ride #{ride.Id}");

                Console.WriteLine(
                    $"Ancien PaymentId : {payment.Id}");

                Console.WriteLine(
                    $"Ancien statut    : {payment.Status}");

                // ========================================================
                // CREER UN NOUVEAU PAYMENT
                // ========================================================

                payment = new Payment
                {
                    RideId =
                        ride.Id,

                    Amount =
                        ride.Price,

                    Currency =
                        "Ar",

                    PaymentMethod =
                        "MVola",

                    Status =
                        "Pending",

                    DebitMsisdn =
                        debitMsisdn,

                    CreditMsisdn =
                        merchantNumber,

                    Description =
                        $"Paiement MoraTUK - Course #{ride.Id} - Nouvelle tentative",

                    CreatedAt =
                        DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                Console.WriteLine(
                    $"Nouveau PaymentId : {payment.Id}");
            }

                // ========================================================
                // EVITER DOUBLE APPEL MVOLA
                // ========================================================

                if (!string.IsNullOrWhiteSpace(
                    payment.ServerCorrelationId))
                {
                    return BadRequest(new
                    {
                        success = false,

                        message =
                            "Un paiement MVola a déjà été lancé pour cette tentative.",

                        serverCorrelationId =
                            payment.ServerCorrelationId,

                        paymentStatus =
                            payment.Status
                    });
                }

            // ========================================================
            // REFERENCE
            // ========================================================

            var transactionReference =
                $"MORATUK-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{ride.Id}";

            // ========================================================
            // REQUETE MVOLA
            // ========================================================

            var mvolaRequest =
                new MvolaPaymentRequest
                {
                    Amount =
                        ride.Price.ToString("0"),

                    Currency =
                        "Ar",

                    DescriptionText =
                        $"Paiement MoraTUK - Course {ride.Id}",

                    RequestingOrganisationTransactionReference =
                        transactionReference,

                    RequestDate =
                        DateTime.UtcNow
                            .ToString(
                                "yyyy-MM-ddTHH:mm:ss.fffZ"),

                    DebitParty =
                        new List<MvolaParty>
                        {
                            new MvolaParty
                            {
                                Key = "msisdn",
                                Value = debitMsisdn
                            }
                        },

                    CreditParty =
                        new List<MvolaParty>
                        {
                            new MvolaParty
                            {
                                Key = "msisdn",
                                Value = merchantNumber
                            }
                        },

                    Metadata =
                        new List<MvolaMetadata>
                        {
                            new MvolaMetadata
                            {
                                Key = "partnerName",
                                Value = "MoraTUK"
                            }
                        }
                };

            // ========================================================
            // DEBUG
            // ========================================================

            Console.WriteLine();
            Console.WriteLine(
                "========== MORATUK -> MVOLA ==========");

            Console.WriteLine(
                $"RideId       : {ride.Id}");

            Console.WriteLine(
                $"PaymentId    : {payment.Id}");

            Console.WriteLine(
                $"Amount       : {mvolaRequest.Amount}");

            Console.WriteLine(
                $"Debit        : {debitMsisdn}");

            Console.WriteLine(
                $"Credit       : {merchantNumber}");

            Console.WriteLine(
                $"Reference    : {transactionReference}");

            Console.WriteLine(
                $"RequestDate  : {mvolaRequest.RequestDate}");

            Console.WriteLine(
                "=======================================");

            // ========================================================
            // APPEL MVOLA
            // ========================================================

            string mvolaResult;

            try
            {
                mvolaResult =
                    await _mvolaService
                        .MerchantPayAsync(
                            mvolaRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "========== ERREUR MVOLA ==========");

                Console.WriteLine(
                    ex.ToString());

                Console.WriteLine(
                    "===================================");

                return StatusCode(502, new
                {
                    success = false,

                    message =
                        "Le paiement MVola n'a pas pu être lancé.",

                    rideId =
                        ride.Id,

                    paymentId =
                        payment.Id,

                    rideStatus =
                        ride.Status,

                    paymentStatus =
                        payment.Status,

                    debitMsisdn =
                        debitMsisdn,

                    creditMsisdn =
                        merchantNumber,

                    error =
                        ex.Message,

                    innerException =
                        ex.InnerException?.Message
                });
            }

            Console.WriteLine(
                $"MVola result : {mvolaResult}");

            // ========================================================
            // REPONSE VIDE
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                mvolaResult))
            {
                return StatusCode(502, new
                {
                    success = false,

                    message =
                        "MVola a retourné une réponse vide.",

                    rideId =
                        ride.Id,

                    paymentId =
                        payment.Id
                });
            }

            // ========================================================
            // PARSER
            // ========================================================

            string? serverCorrelationId = null;

            try
            {
                using var mvolaJson =
                    JsonDocument.Parse(
                        mvolaResult);

                var root =
                    mvolaJson.RootElement;

                if (root.TryGetProperty(
                    "serverCorrelationId",
                    out var correlationProperty))
                {
                    serverCorrelationId =
                        correlationProperty
                            .GetString();
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"REPONSE MVOLA NON JSON : {ex}");

                return StatusCode(502, new
                {
                    success = false,

                    message =
                        "La réponse MVola n'est pas un JSON valide.",

                    rideId =
                        ride.Id,

                    paymentId =
                        payment.Id,

                    mvolaResponse =
                        mvolaResult,

                    error =
                        ex.Message
                });
            }

            // ========================================================
            // CORRELATION ID
            // ========================================================

            if (string.IsNullOrWhiteSpace(
                serverCorrelationId))
            {
                return StatusCode(502, new
                {
                    success = false,

                    message =
                        "MVola n'a pas retourné de serverCorrelationId.",

                    rideId =
                        ride.Id,

                    paymentId =
                        payment.Id,

                    mvolaResponse =
                        mvolaResult
                });
            }

            // ========================================================
            // SAUVEGARDE
            // ========================================================

            payment.TransactionReference =
                transactionReference;

            payment.ServerCorrelationId =
                serverCorrelationId;

            payment.Status =
                "Pending";

            payment.UpdatedAt =
                DateTime.UtcNow;

            ride.Status =
                "Accepted";

            driver.IsAvailable =
                false;

            await _context.SaveChangesAsync();

            Console.WriteLine(
                "Paiement MVola lancé avec succès.");

            Console.WriteLine(
                $"CorrelationId : {serverCorrelationId}");

            // ========================================================
            // CLIENT
            // ========================================================

            await _hub.Clients
                .Group($"client-{ride.ClientId}")
                .SendAsync(
                    "RideAccepted",
                    new
                    {
                        rideId =
                            ride.Id,

                        driverId =
                            driverId,

                        status =
                            ride.Status,

                        paymentStatus =
                            payment.Status,

                        serverCorrelationId =
                            payment.ServerCorrelationId
                    });

            return Ok(new
            {
                success = true,

                message =
                    "Course acceptée. Paiement MVola envoyé et en attente de confirmation.",

                rideId =
                    ride.Id,

                driverId =
                    driverId,

                status =
                    ride.Status,

                paymentId =
                    payment.Id,

                paymentStatus =
                    payment.Status,

                serverCorrelationId =
                    payment.ServerCorrelationId,

                transactionReference =
                    payment.TransactionReference
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR ACCEPT RIDE : {ex}");

            return StatusCode(500, new
            {
                success = false,

                message =
                    "Erreur lors de l'acceptation de la course.",

                error =
                    ex.Message,

                innerException =
                    ex.InnerException?.Message
            });
        }
    }

    // ============================================================
    // REFUSER COURSE
    // ============================================================

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectRide(
        int id,
        [FromQuery] int driverId)
    {
        try
        {
            var ride = await _context.Rides
                .FirstOrDefaultAsync(
                    x => x.Id == id);

            if (ride == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Course introuvable."
                });
            }

            if (ride.DriverId != driverId)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Cette course n'est pas affectée à ce chauffeur."
                });
            }

            if (ride.Status != "WaitingDriver")
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Cette course n'est plus disponible."
                });
            }

            var currentDriver =
                await _context.Drivers
                    .FirstOrDefaultAsync(
                        x => x.Id == driverId);

            if (currentDriver != null)
            {
                currentDriver.IsAvailable =
                    true;
            }

            var driverLocations =
                await _context.DriverLocations
                    .GroupBy(x => x.DriverId)
                    .Select(g => g
                        .OrderByDescending(
                            x => x.CreatedAt)
                        .First())
                    .ToListAsync();

            var availableDrivers =
                await _context.Drivers
                    .Include(d => d.User)
                    .Where(d =>
                        d.IsAvailable &&
                        d.Id != driverId)
                    .ToListAsync();

            var nextDriver =
                availableDrivers
                    .Join(
                        driverLocations,
                        driver => driver.Id,
                        location => location.DriverId,
                        (driver, location) => new
                        {
                            Driver = driver,

                            Distance =
                                _distanceService.Calculate(
                                    ride.PickupLatitude,
                                    ride.PickupLongitude,
                                    location.Latitude,
                                    location.Longitude)
                        })
                    .Where(x => x.Distance <= 5)
                    .OrderBy(x => x.Distance)
                    .FirstOrDefault();

            if (nextDriver == null)
            {
                ride.DriverId = null;

                ride.Status =
                    "WaitingDriver";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,

                    message =
                        "Course refusée. Aucun autre chauffeur disponible.",

                    rideId =
                        ride.Id,

                    driverId =
                        (int?)null,

                    status =
                        ride.Status
                });
            }

            ride.DriverId =
                nextDriver.Driver.Id;

            ride.Status =
                "WaitingDriver";

            await _context.SaveChangesAsync();

            await _hub.Clients
                .Group(
                    $"driver-{nextDriver.Driver.Id}")
                .SendAsync(
                    "NewRide",
                    new
                    {
                        RideId =
                            ride.Id,

                        DriverId =
                            nextDriver.Driver.Id,

                        PickupLatitude =
                            ride.PickupLatitude,

                        PickupLongitude =
                            ride.PickupLongitude,

                        DestinationLatitude =
                            ride.DestinationLatitude,

                        DestinationLongitude =
                            ride.DestinationLongitude,

                        Departure =
                            ride.Departure,

                        Destination =
                            ride.Destination,

                        Price =
                            ride.Price,

                        RideType =
                            ride.RideType,

                        Passengers =
                            ride.CurrentPassengers,

                        DistanceToDriver =
                            nextDriver.Distance,

                        Status =
                            ride.Status
                    });

            return Ok(new
            {
                success = true,

                message =
                    "Course transférée au chauffeur suivant.",

                rideId =
                    ride.Id,

                previousDriverId =
                    driverId,

                newDriverId =
                    nextDriver.Driver.Id,

                newDriver =
                    nextDriver.Driver.User?.FullName,

                distanceKm =
                    Math.Round(
                        nextDriver.Distance,
                        2),

                price =
                    ride.Price,

                status =
                    ride.Status
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR REJECT RIDE : {ex}");

            return StatusCode(500, new
            {
                success = false,

                message =
                    "Erreur lors du refus de la course.",

                error =
                    ex.Message
            });
        }
    }

    // ============================================================
    // STATUT PAIEMENT
    // ============================================================

    [HttpGet("{id}/payment-status")]
    public async Task<IActionResult> GetPaymentStatus(
        int id)
    {
        try
        {
            var ride =
                await _context.Rides
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (ride == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Course introuvable."
                });
            }

            var payment =
                await _context.Payments
                    .Where(x => x.RideId == ride.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

            if (payment == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Paiement introuvable."
                });
            }

            if (string.IsNullOrWhiteSpace(
                payment.ServerCorrelationId))
            {
                return Ok(new
                {
                    success = true,

                    confirmed = false,

                    pending = true,

                    rideId =
                        ride.Id,

                    paymentId =
                        payment.Id,

                    rideStatus =
                        ride.Status,

                    paymentStatus =
                        payment.Status,

                    mvolaStatus =
                        (string?)null,

                    message =
                        "Paiement MVola pas encore lancé."
                });
            }

            // ========================================================
            // APPEL MVOLA
            // ========================================================

            var mvolaResult =
                await _mvolaService
                    .GetPaymentStatusAsync(
                        payment.ServerCorrelationId);

            Console.WriteLine();
            Console.WriteLine(
                "========== MVOLA PAYMENT STATUS ==========");

            Console.WriteLine(
                $"RideId : {ride.Id}");

            Console.WriteLine(
                $"PaymentId : {payment.Id}");

            Console.WriteLine(
                $"CorrelationId : " +
                $"{payment.ServerCorrelationId}");

            Console.WriteLine(
                $"Response : {mvolaResult}");

            Console.WriteLine(
                "===========================================");

            string? mvolaStatus = null;

            try
            {
                using var statusJson =
                    JsonDocument.Parse(
                        mvolaResult);

                var root =
                    statusJson.RootElement;

                string[] possibleProperties =
                {
                    "status",
                    "transactionStatus",
                    "paymentStatus",
                    "state"
                };

                foreach (var propertyName
                         in possibleProperties)
                {
                    if (root.TryGetProperty(
                        propertyName,
                        out var property))
                    {
                        if (property.ValueKind ==
                            JsonValueKind.String)
                        {
                            mvolaStatus =
                                property.GetString();
                        }

                        if (!string.IsNullOrWhiteSpace(
                            mvolaStatus))
                        {
                            break;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"ERREUR PARSING MVOLA STATUS : {ex}");

                return Ok(new
                {
                    success = false,

                    confirmed = false,

                    pending = true,

                    rideId =
                        ride.Id,

                    paymentId =
                        payment.Id,

                    rideStatus =
                        ride.Status,

                    paymentStatus =
                        payment.Status,

                    message =
                        "Réponse MVola impossible à interpréter.",

                    error =
                        ex.Message,

                    result =
                        mvolaResult
                });
            }

            // ========================================================
            // NORMALISATION
            // ========================================================

            var normalizedStatus =
                mvolaStatus?
                    .Trim()
                    .ToUpperInvariant();

            // ========================================================
            // SUCCESS
            // ========================================================

            if (
                normalizedStatus == "SUCCESS" ||
                normalizedStatus == "SUCCESSFUL" ||
                normalizedStatus == "COMPLETED" ||
                normalizedStatus == "CONFIRMED" ||
                normalizedStatus == "COMPLETED_SUCCESSFULLY")
            {
                payment.Status =
                    "Success";

                payment.UpdatedAt =
                    DateTime.UtcNow;

                if (ride.Status == "Accepted")
                {
                    ride.Status =
                        "InProgress";
                }

                await _context.SaveChangesAsync();

                await _hub.Clients
                    .Group(
                        $"client-{ride.ClientId}")
                    .SendAsync(
                        "PaymentConfirmed",
                        new
                        {
                            rideId =
                                ride.Id,

                            paymentId =
                                payment.Id,

                            rideStatus =
                                ride.Status,

                            paymentStatus =
                                payment.Status
                        });

                return Ok(new
                {
                    success = true,

                    confirmed = true,

                    pending = false,

                    failed = false,

                    rideId =
                        ride.Id,

                    paymentId =
                        payment.Id,

                    rideStatus =
                        ride.Status,

                    paymentStatus =
                        payment.Status,

                    mvolaStatus =
                        mvolaStatus,

                    serverCorrelationId =
                        payment.ServerCorrelationId,

                    result =
                        mvolaResult
                });
            }

            // ========================================================
            // FAILED
            // ========================================================

            if (
                normalizedStatus == "FAILED" ||
                normalizedStatus == "FAILURE" ||
                normalizedStatus == "REJECTED" ||
                normalizedStatus == "CANCELLED" ||
                normalizedStatus == "CANCELED")
            {
                payment.Status = "Failed";

                payment.UpdatedAt = DateTime.UtcNow;

                // ========================================================
                // LIBERER LE CHAUFFEUR
                // ========================================================

                if (ride.DriverId.HasValue)
                {
                    var driver =
                        await _context.Drivers
                            .FirstOrDefaultAsync(
                                x => x.Id == ride.DriverId.Value);

                    if (driver != null)
                    {
                        driver.IsAvailable = true;
                    }
                }

                // ========================================================
                // IMPORTANT :
                // Le paiement a échoué mais la course peut être
                // retentée par le chauffeur.
                // ========================================================

                ride.Status = "WaitingDriver";

                await _context.SaveChangesAsync();

                // ========================================================
                // NOTIFICATION CLIENT
                // ========================================================

                await _hub.Clients
                    .Group($"client-{ride.ClientId}")
                    .SendAsync(
                        "PaymentFailed",
                        new
                        {
                            rideId = ride.Id,

                            paymentId = payment.Id,

                            rideStatus = ride.Status,

                            paymentStatus = payment.Status,

                            mvolaStatus = mvolaStatus,

                            retryAllowed = true
                        });

                // ========================================================
                // NOTIFICATION CHAUFFEUR
                //
                // Permet au mobile de remettre la course dans son état
                // d'attente.
                // ========================================================

                if (ride.DriverId.HasValue)
                {
                    await _hub.Clients
                        .Group($"driver-{ride.DriverId.Value}")
                        .SendAsync(
                            "PaymentFailed",
                            new
                            {
                                rideId = ride.Id,

                                driverId = ride.DriverId.Value,

                                rideStatus = ride.Status,

                                paymentStatus = payment.Status,

                                mvolaStatus = mvolaStatus,

                                retryAllowed = true
                            });
                }

                return Ok(new
                {
                    success = true,

                    confirmed = false,

                    pending = false,

                    failed = true,

                    retryAllowed = true,

                    rideId = ride.Id,

                    paymentId = payment.Id,

                    rideStatus = ride.Status,

                    paymentStatus = payment.Status,

                    mvolaStatus = mvolaStatus,

                    serverCorrelationId =
                        payment.ServerCorrelationId,

                    result = mvolaResult
                });
            }

            // ========================================================
            // PENDING
            // ========================================================

            payment.Status =
                "Pending";

            payment.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,

                confirmed = false,

                pending = true,

                failed = false,

                rideId =
                    ride.Id,

                paymentId =
                    payment.Id,

                rideStatus =
                    ride.Status,

                paymentStatus =
                    payment.Status,

                mvolaStatus =
                    mvolaStatus,

                serverCorrelationId =
                    payment.ServerCorrelationId,

                result =
                    mvolaResult
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR PAYMENT STATUS : {ex}");

            return StatusCode(500, new
            {
                success = false,

                message =
                    "Erreur lors de la vérification du paiement MVola.",

                error =
                    ex.Message,

                innerException =
                    ex.InnerException?.Message
            });
        }
    }

    // ============================================================
    // TERMINER COURSE
    // ============================================================

    [HttpPut("{id}/complete")]
    public async Task<IActionResult> CompleteRide(
        int id,
        [FromQuery] int driverId)
    {
        try
        {
            var ride =
                await _context.Rides
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (ride == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Course introuvable."
                });
            }

            if (ride.DriverId != driverId)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Cette course n'est pas affectée à ce chauffeur."
                });
            }

            if (ride.Status != "InProgress")
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        $"Impossible de terminer la course. " +
                        $"Statut actuel : {ride.Status}"
                });
            }

            var driver =
                await _context.Drivers
                    .FirstOrDefaultAsync(
                        x => x.Id == driverId);

            if (driver == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Chauffeur introuvable."
                });
            }

            var payment =
                await _context.Payments
                    .FirstOrDefaultAsync(
                        x => x.RideId == ride.Id);

            if (payment == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Paiement introuvable."
                });
            }

            if (payment.Status != "Success" &&
                payment.Status != "Completed")
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Le paiement MVola n'est pas confirmé.",

                    paymentStatus =
                        payment.Status
                });
            }

            // ========================================================
            // TERMINER
            // ========================================================

            ride.Status =
                "Completed";

            driver.IsAvailable =
                true;

            await _context.SaveChangesAsync();

            // ========================================================
            // CLIENT
            // ========================================================

            await _hub.Clients
                .Group(
                    $"client-{ride.ClientId}")
                .SendAsync(
                    "RideCompleted",
                    new
                    {
                        rideId =
                            ride.Id,

                        driverId =
                            driverId,

                        status =
                            ride.Status,

                        paymentStatus =
                            payment.Status,

                        message =
                            "Votre course est terminée."
                    });

            return Ok(new
            {
                success = true,

                message =
                    "Course terminée avec succès.",

                rideId =
                    ride.Id,

                driverId =
                    driverId,

                status =
                    ride.Status,

                paymentStatus =
                    payment.Status,

                driverAvailable =
                    driver.IsAvailable
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR COMPLETE RIDE : {ex}");

            return StatusCode(500, new
            {
                success = false,

                message =
                    "Erreur lors de la clôture de la course.",

                error =
                    ex.Message,

                innerException =
                    ex.InnerException?.Message
            });
        }
    }

    // ============================================================
    // COURSES DU CHAUFFEUR
    // ============================================================

    [HttpGet("available/{driverId:int}")]
    public async Task<IActionResult>
        GetAvailableRidesForDriver(
            int driverId)
    {
        try
        {
            var activeRide =
                await _context.Rides
                    .FirstOrDefaultAsync(x =>
                        x.DriverId == driverId &&
                        (
                            x.Status == "Accepted" ||
                            x.Status == "InProgress"
                        ));

            if (activeRide != null)
            {
                var activeRides =
                    await _context.Rides
                        .Where(x =>
                            x.DriverId == driverId &&
                            (
                                x.Status == "Accepted" ||
                                x.Status == "InProgress"
                            ))
                        .OrderByDescending(
                            x => x.CreatedAt)
                        .Select(x => new
                        {
                            RideId =
                                x.Id,

                            PickupLatitude =
                                x.PickupLatitude,

                            PickupLongitude =
                                x.PickupLongitude,

                            DestinationLatitude =
                                x.DestinationLatitude,

                            DestinationLongitude =
                                x.DestinationLongitude,

                            Price =
                                x.Price,

                            RideType =
                                x.RideType,

                            Passengers =
                                x.CurrentPassengers,

                            DistanceToDriver =
                                0,

                            Departure =
                                x.Departure,

                            Destination =
                                x.Destination,

                            DriverId =
                                x.DriverId,

                            Status =
                                x.Status
                        })
                        .ToListAsync();

                return Ok(activeRides);
            }

            var rides =
                await _context.Rides
                    .Where(x =>
                        x.DriverId == driverId &&
                        x.Status == "WaitingDriver")
                    .OrderByDescending(
                        x => x.CreatedAt)
                    .Select(x => new
                    {
                        RideId =
                            x.Id,

                        PickupLatitude =
                            x.PickupLatitude,

                        PickupLongitude =
                            x.PickupLongitude,

                        DestinationLatitude =
                            x.DestinationLatitude,

                        DestinationLongitude =
                            x.DestinationLongitude,

                        Price =
                            x.Price,

                        RideType =
                            x.RideType,

                        Passengers =
                            x.CurrentPassengers,

                        DistanceToDriver =
                            0,

                        Departure =
                            x.Departure,

                        Destination =
                            x.Destination,

                        DriverId =
                            x.DriverId,

                        Status =
                            x.Status
                    })
                    .ToListAsync();

            return Ok(rides);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR GET RIDES : {ex}");

            return StatusCode(500, new
            {
                success = false,

                message =
                    "Erreur lors de la récupération des courses.",

                error =
                    ex.Message
            });
        }
    }

    // ============================================================
    // COURSE ACTIVE
    // ============================================================

    [HttpGet("active/{driverId:int}")]
    public async Task<IActionResult>
        GetActiveRideForDriver(
            int driverId)
    {
        try
        {
            var ride =
                await _context.Rides
                    .Where(x =>
                        x.DriverId == driverId &&
                        (
                            x.Status == "Accepted" ||
                            x.Status == "InProgress"
                        ))
                    .OrderByDescending(
                        x => x.CreatedAt)
                    .Select(x => new
                    {
                        RideId =
                            x.Id,

                        DriverId =
                            x.DriverId,

                        PickupLatitude =
                            x.PickupLatitude,

                        PickupLongitude =
                            x.PickupLongitude,

                        DestinationLatitude =
                            x.DestinationLatitude,

                        DestinationLongitude =
                            x.DestinationLongitude,

                        Departure =
                            x.Departure,

                        Destination =
                            x.Destination,

                        Price =
                            x.Price,

                        RideType =
                            x.RideType,

                        Passengers =
                            x.CurrentPassengers,

                        DistanceToDriver =
                            0,

                        Status =
                            x.Status
                    })
                    .FirstOrDefaultAsync();

            return Ok(ride);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR GET ACTIVE RIDE : {ex}");

            return StatusCode(500, new
            {
                success = false,

                message =
                    "Erreur lors de la récupération de la course active.",

                error =
                    ex.Message
            });
        }
    }

    // ============================================================
    // CALCUL PRIX
    // ============================================================

    private static decimal CalculatePrice(
        double distance,
        string? rideType)
    {
        var price =
            Math.Ceiling(
                (decimal)distance *
                PRICE_PER_KM /
                100m) *
            100m;

        if (price < MIN_RIDE_PRICE)
        {
            price =
                MIN_RIDE_PRICE;
        }

        if (string.Equals(
            rideType,
            "Private",
            StringComparison.OrdinalIgnoreCase))
        {
            price *= 4;
        }

        return price;
    }

    // ============================================================
    // NORMALISATION MSISDN
    // ============================================================

    private static string NormalizeMsisdn(
        string value)
    {
        var msisdn =
            value.Trim();

        if (msisdn.StartsWith(
            "msisdn;",
            StringComparison.OrdinalIgnoreCase))
        {
            msisdn =
                msisdn.Substring(
                    "msisdn;".Length);
        }

        return msisdn.Trim();
    }
}