using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MoraTuk.API.Data;
using MoraTuk.API.Models;
using MoraTuk.API.Services;
using MoraTuk.API.Hubs;

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
                return BadRequest(
                    "Les données de la course sont obligatoires.");

            var distance = _distanceService.Calculate(
                ride.PickupLatitude,
                ride.PickupLongitude,
                ride.DestinationLatitude,
                ride.DestinationLongitude);

            if (distance <= 0)
                return BadRequest(
                    "La distance calculée est invalide.");

            decimal price = Math.Ceiling(
                (decimal)distance * PRICE_PER_KM / 100m
            ) * 100m;

            if (price < MIN_RIDE_PRICE)
                price = MIN_RIDE_PRICE;

            if (string.Equals(
                ride.RideType,
                "Private",
                StringComparison.OrdinalIgnoreCase))
            {
                price *= 4;
            }

            return Ok(new
            {
                distanceKm = Math.Round(distance, 2),
                price
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR ESTIMATION : {ex}");

            return StatusCode(
                500,
                new
                {
                    message = "Erreur lors du calcul du prix.",
                    error = ex.Message
                });
        }
    }

    // ============================================================
    // CREATION COURSE
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

            if (string.IsNullOrWhiteSpace(request.DebitMsisdn))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Le numéro MVola du client est obligatoire."
                });
            }

            // ========================================================
            // CLIENT
            // ========================================================

            var client = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == request.ClientId);

            if (client == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Client introuvable."
                });
            }

            if (client.Role != "Client")
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
            // DERNIÈRES POSITIONS DES CHAUFFEURS
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
                    message = "Aucun chauffeur avec une position connue."
                });
            }

            Console.WriteLine(
                $"Driver {nearestDriver.Driver.Id} - " +
                $"Distance : {nearestDriver.Distance} km");

            if (nearestDriver.Distance > 5)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Aucun chauffeur disponible dans un rayon de 5 km."
                });
            }

            // ========================================================
            // DISTANCE DE LA COURSE
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
            // CALCUL DU PRIX
            // ========================================================

            decimal price = Math.Ceiling(
                (decimal)distance * PRICE_PER_KM / 100m
            ) * 100m;

            if (price < MIN_RIDE_PRICE)
                price = MIN_RIDE_PRICE;

            if (string.Equals(
                request.RideType,
                "Private",
                StringComparison.OrdinalIgnoreCase))
            {
                price *= 4;
            }

            // ========================================================
            // CREATION RIDE
            // ========================================================

            var ride = new Ride
            {
                ClientId = request.ClientId,

                DriverId = nearestDriver.Driver.Id,

                PickupLatitude = request.PickupLatitude,
                PickupLongitude = request.PickupLongitude,
                Departure = request.Departure,

                DestinationLatitude = request.DestinationLatitude,
                DestinationLongitude = request.DestinationLongitude,
                Destination = request.Destination,

                Price = price,

                RideType = string.IsNullOrWhiteSpace(request.RideType)
                    ? "Shared"
                    : request.RideType,

                MaxPassengers = 4,
                CurrentPassengers = 1,

                Status = "WaitingDriver",

                CreatedAt = DateTime.UtcNow
            };

            _context.Rides.Add(ride);

            await _context.SaveChangesAsync();

            Console.WriteLine(
                $"Course créée : {ride.Id}");

            // ========================================================
            // CREATION PAYMENT
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

            var payment = new Payment
            {
                RideId = ride.Id,

                Amount = ride.Price,

                Currency = "Ar",

                PaymentMethod = "MVola",

                Status = "Pending",

                DebitMsisdn = request.DebitMsisdn,

                CreditMsisdn = merchantNumber,

                Description =
                    $"Paiement MoraTUK - Course #{ride.Id}",

                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            Console.WriteLine(
                $"Payment créé : {payment.Id}");

            // ========================================================
            // APPEL MVOLA
            // ========================================================

            var transactionReference =
                $"MORATUK-{DateTime.UtcNow:yyyyMMddHHmmss}-{ride.Id}";

            var mvolaRequest = new MvolaPaymentRequest
            {
                Amount = ride.Price.ToString("0"),

                Currency = "Ar",

                DescriptionText =
                    $"Paiement MoraTUK - Course {ride.Id}",

                RequestingOrganisationTransactionReference =
                    transactionReference,

                RequestDate =
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),

                DebitParty = new List<MvolaParty>
                {
                    new MvolaParty
                    {
                        Key = "msisdn",
                        Value = request.DebitMsisdn
                    }
                },

                CreditParty = new List<MvolaParty>
                {
                    new MvolaParty
                    {
                        Key = "msisdn",
                        Value = merchantNumber
                    }
                },

                Metadata = new List<MvolaMetadata>
                {
                    new MvolaMetadata
                    {
                        Key = "partnerName",
                        Value = "MoraTUK"
                    }
                }
            };

            Console.WriteLine();
            Console.WriteLine("========== MORATUK → MVOLA ==========");
            Console.WriteLine($"RideId       : {ride.Id}");
            Console.WriteLine($"Amount       : {mvolaRequest.Amount}");
            Console.WriteLine($"Currency     : {mvolaRequest.Currency}");
            Console.WriteLine($"Description  : {mvolaRequest.DescriptionText}");
            Console.WriteLine($"Reference    : {mvolaRequest.RequestingOrganisationTransactionReference}");
            Console.WriteLine($"RequestDate  : {mvolaRequest.RequestDate}");

            Console.WriteLine(
                $"Debit        : {mvolaRequest.DebitParty.FirstOrDefault()?.Key} = {mvolaRequest.DebitParty.FirstOrDefault()?.Value}");

            Console.WriteLine(
                $"Credit       : {mvolaRequest.CreditParty.FirstOrDefault()?.Key} = {mvolaRequest.CreditParty.FirstOrDefault()?.Value}");

            foreach (var metadata in mvolaRequest.Metadata)
            {
                Console.WriteLine(
                    $"Metadata     : {metadata.Key} = {metadata.Value}");
            }

            Console.WriteLine("=====================================");

            var mvolaResult =
                await _mvolaService.MerchantPayAsync(
                    mvolaRequest);

            Console.WriteLine(
                $"MVola result : {mvolaResult}");

            // ========================================================
            // RECUPERER SERVER CORRELATION ID
            // ========================================================

            using var mvolaJson =
                System.Text.Json.JsonDocument.Parse(
                    mvolaResult);

            if (mvolaJson.RootElement.TryGetProperty(
                "serverCorrelationId",
                out var correlationProperty))
            {
                payment.ServerCorrelationId =
                    correlationProperty.GetString();
            }

            payment.TransactionReference =
                transactionReference;

            payment.Status = "Pending";

            payment.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ========================================================
            // NOTIFICATION SIGNALR
            // ========================================================

            await _hub.Clients
                .Group($"driver-{nearestDriver.Driver.Id}")
                .SendAsync(
                    "NewRide",
                    new
                    {
                        RideId = ride.Id,

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
                            ride.Destination
                    });

            // ========================================================
            // REPONSE
            // ========================================================

            return Ok(new
            {
                success = true,

                message = "Course créée et paiement MVola envoyé.",

                rideId = ride.Id,

                paymentId = payment.Id,

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

            return StatusCode(
                500,
                new
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
    // ============================================================

    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptRide(
        int id,
        int driverId)
    {
        var existingActiveRide = await _context.Rides
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
                activeRideId = existingActiveRide.Id,
                activeRideStatus = existingActiveRide.Status
            });
        }

        var ride = await _context.Rides
            .FirstOrDefaultAsync(
                x => x.Id == id);

        if (ride == null)
            return NotFound(
                "Course introuvable.");

        if (ride.Status != "WaitingDriver")
            return BadRequest(
                "Cette course n'est plus disponible.");
        if (ride.DriverId != driverId)
            {
                return BadRequest(
                    "Cette course n'est pas affectée à ce chauffeur.");
            }

        var driver = await _context.Drivers
            .FirstOrDefaultAsync(
                x => x.Id == driverId);

        if (driver == null)
            return BadRequest(
                "Chauffeur introuvable.");

        if (!driver.IsAvailable)
            return BadRequest(
                "Ce chauffeur est déjà occupé.");

        ride.DriverId = driverId;
        ride.Status = "Accepted";

        driver.IsAvailable = false;

        await _context.SaveChangesAsync();

        await _hub.Clients
            .Group($"client-{ride.ClientId}")
            .SendAsync(
                "RideAccepted",
                new
                {
                    rideId = ride.Id,
                    driverId,
                    status = "Accepted"
                });

        return Ok(new
        {
            message = "Course acceptée",
            rideId = ride.Id,
            driverId,
            status = ride.Status
        });
    }
// ============================================================
// REFUSER COURSE
// ============================================================

[HttpPut("{id}/reject")]
public async Task<IActionResult> RejectRide(
    int id,
    int driverId)
{
    try
    {
        // --------------------------------------------------------
        // COURSE
        // --------------------------------------------------------

        var ride = await _context.Rides
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ride == null)
        {
            return NotFound(
                "Course introuvable.");
        }

        // --------------------------------------------------------
        // VÉRIFIER LE CHAUFFEUR
        // --------------------------------------------------------

        if (ride.DriverId != driverId)
        {
            return BadRequest(
                "Cette course n'est pas affectée à ce chauffeur.");
        }

        // --------------------------------------------------------
        // LA COURSE DOIT ÊTRE EN ATTENTE
        // --------------------------------------------------------

        if (ride.Status != "WaitingDriver")
        {
            return BadRequest(
                "Cette course n'est plus disponible.");
        }

        // --------------------------------------------------------
        // CHAUFFEUR ACTUEL
        // --------------------------------------------------------

        var currentDriver = await _context.Drivers
            .FirstOrDefaultAsync(
                x => x.Id == driverId);

        if (currentDriver != null)
        {
            currentDriver.IsAvailable = true;
        }

        // --------------------------------------------------------
        // DERNIÈRES POSITIONS DES CHAUFFEURS
        // --------------------------------------------------------

        var driverLocations = await _context.DriverLocations
            .GroupBy(x => x.DriverId)
            .Select(g => g
                .OrderByDescending(x => x.CreatedAt)
                .First())
            .ToListAsync();

        // --------------------------------------------------------
        // CHERCHE LES AUTRES CHAUFFEURS
        // --------------------------------------------------------

        var availableDrivers = await _context.Drivers
            .Include(d => d.User)
            .Where(d =>
                d.IsAvailable &&
                d.Id != driverId)
            .ToListAsync();

        // --------------------------------------------------------
        // CHAUFFEURS AVEC POSITION
        // --------------------------------------------------------

        var nextDriver = availableDrivers
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
            .Where(x =>
                x.Distance <= 5)
            .OrderBy(x =>
                x.Distance)
            .FirstOrDefault();

        // --------------------------------------------------------
        // AUCUN AUTRE CHAUFFEUR
        // --------------------------------------------------------

        if (nextDriver == null)
        {
            ride.DriverId = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Course refusée. " +
                    "Aucun autre chauffeur disponible.",

                rideId = ride.Id,

                driverId = (int?)null,

                status = ride.Status
            });
        }

        // --------------------------------------------------------
        // AFFECTER AU NOUVEAU CHAUFFEUR
        // --------------------------------------------------------

        ride.DriverId =
            nextDriver.Driver.Id;

        ride.Status =
            "WaitingDriver";

        await _context.SaveChangesAsync();

        Console.WriteLine(
            $"Course #{ride.Id} refusée par Driver {driverId}");

        Console.WriteLine(
            $"Nouvel chauffeur : " +
            $"Driver {nextDriver.Driver.Id}");

        Console.WriteLine(
            $"Distance : " +
            $"{nextDriver.Distance:F2} km");

        // --------------------------------------------------------
        // NOTIFIER LE NOUVEAU CHAUFFEUR
        // --------------------------------------------------------

        await _hub.Clients
            .Group(
                $"driver-{nextDriver.Driver.Id}")
            .SendAsync(
                "NewRide",
                new
                {
                    RideId = ride.Id,

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
                        nextDriver.Distance
                });

        // --------------------------------------------------------
        // RÉPONSE
        // --------------------------------------------------------

        return Ok(new
        {
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

        return StatusCode(
            500,
            new
            {
                message =
                    "Erreur lors du refus de la course.",

                error =
                    ex.Message
            });
    }
}
// ============================================================
// TERMINER COURSE
// ============================================================

    [HttpPut("{id}/complete")]
    public async Task<IActionResult> CompleteRide(
        int id,
        int driverId)
    {
        try
        {
            Console.WriteLine(
                "====================================");

            Console.WriteLine(
                $"TERMINER COURSE #{id}");

            Console.WriteLine(
                $"DriverId : {driverId}");

            // --------------------------------------------------------
            // COURSE
            // --------------------------------------------------------

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

            // --------------------------------------------------------
            // VÉRIFIER LE CHAUFFEUR
            // --------------------------------------------------------

            if (ride.DriverId != driverId)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Cette course n'est pas affectée à ce chauffeur."
                });
            }

            // --------------------------------------------------------
            // LA COURSE DOIT ÊTRE ACCEPTÉE
            // --------------------------------------------------------

            if (ride.Status != "Accepted")
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        $"Impossible de terminer la course. " +
                        $"Statut actuel : {ride.Status}"
                });
            }

            // --------------------------------------------------------
            // CHAUFFEUR
            // --------------------------------------------------------

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

            // --------------------------------------------------------
            // TERMINER LA COURSE
            // --------------------------------------------------------

            ride.Status = "Completed";

            // --------------------------------------------------------
            // RENDRE LE CHAUFFEUR DISPONIBLE
            // --------------------------------------------------------

            driver.IsAvailable = true;

            await _context.SaveChangesAsync();

            Console.WriteLine(
                $"Course #{ride.Id} terminée.");

            Console.WriteLine(
                $"Driver {driverId} est maintenant disponible.");

            // --------------------------------------------------------
            // NOTIFIER LE CLIENT
            // --------------------------------------------------------

            await _hub.Clients
                .Group($"client-{ride.ClientId}")
                .SendAsync(
                    "RideCompleted",
                    new
                    {
                        rideId = ride.Id,

                        driverId = driverId,

                        status = "Completed",

                        message =
                            "Votre course est terminée."
                    });

            Console.WriteLine(
                "Client notifié.");

            Console.WriteLine(
                "====================================");

            // --------------------------------------------------------
            // RÉPONSE
            // --------------------------------------------------------

            return Ok(new
            {
                success = true,

                message =
                    "Course terminée avec succès.",

                rideId = ride.Id,

                driverId = driverId,

                status = ride.Status,

                driverAvailable =
                    driver.IsAvailable
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"ERREUR COMPLETE RIDE : {ex}");

            return StatusCode(
                500,
                new
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
        public async Task<IActionResult> GetAvailableRidesForDriver(int driverId)
        {
            try
            {
                // ========================================================
                // VÉRIFIER SI LE CHAUFFEUR A UNE COURSE EN COURS
                // ========================================================

                var activeRide = await _context.Rides
                    .FirstOrDefaultAsync(x =>
                        x.DriverId == driverId &&
                        (
                            x.Status == "Accepted" ||
                            x.Status == "InProgress"
                        ));

                // ========================================================
                // SI LE CHAUFFEUR A DÉJÀ UNE COURSE
                // ========================================================

                if (activeRide != null)
                {
                    var activeRides = await _context.Rides
                        .Where(x =>
                            x.DriverId == driverId &&
                            (
                                x.Status == "Accepted" ||
                                x.Status == "InProgress"
                            ))
                        .Include(x => x.Client)
                        .OrderByDescending(x => x.CreatedAt)
                        .Select(x => new
                        {
                            RideId = x.Id,

                            PickupLatitude = x.PickupLatitude,
                            PickupLongitude = x.PickupLongitude,

                            DestinationLatitude =
                                x.DestinationLatitude,

                            DestinationLongitude =
                                x.DestinationLongitude,

                            Price = x.Price,

                            RideType = x.RideType,

                            Passengers =
                                x.CurrentPassengers,

                            DistanceToDriver = 0,

                            Departure = x.Departure,
                            Destination = x.Destination,

                            DriverId = x.DriverId,

                            Status = x.Status
                        })
                        .ToListAsync();

                    Console.WriteLine(
                        $"====================================");

                    Console.WriteLine(
                        $"CHAUFFEUR {driverId} OCCUPÉ");

                    Console.WriteLine(
                        $"COURSE ACTIVE : {activeRide.Id}");

                    Console.WriteLine(
                        $"STATUS : {activeRide.Status}");

                    Console.WriteLine(
                        $"Nombre : {activeRides.Count}");

                    Console.WriteLine(
                        $"====================================");

                    return Ok(activeRides);
                }

                // ========================================================
                // CHAUFFEUR LIBRE
                // ========================================================

                var rides = await _context.Rides
                    .Where(x =>
                        x.DriverId == driverId &&
                        x.Status == "WaitingDriver")
                    .Include(x => x.Client)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        RideId = x.Id,

                        PickupLatitude = x.PickupLatitude,
                        PickupLongitude = x.PickupLongitude,

                        DestinationLatitude =
                            x.DestinationLatitude,

                        DestinationLongitude =
                            x.DestinationLongitude,

                        Price = x.Price,

                        RideType = x.RideType,

                        Passengers =
                            x.CurrentPassengers,

                        DistanceToDriver = 0,

                        Departure = x.Departure,
                        Destination = x.Destination,

                        DriverId = x.DriverId,

                        Status = x.Status
                    })
                    .ToListAsync();

                // ========================================================
                // LOG
                // ========================================================

                Console.WriteLine(
                    $"====================================");

                Console.WriteLine(
                    $"CHAUFFEUR {driverId} DISPONIBLE");

                Console.WriteLine(
                    $"COURSES DISPONIBLES : {rides.Count}");

                foreach (var ride in rides)
                {
                    Console.WriteLine(
                        $"Ride #{ride.RideId} | " +
                        $"Driver={ride.DriverId} | " +
                        $"Status={ride.Status}");
                }

                Console.WriteLine(
                    $"====================================");

                return Ok(rides);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"ERREUR GET ACTIVE RIDES : {ex}");

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "Erreur lors de la récupération des courses.",
                        error = ex.Message
                    });
            }
        }
        // ============================================================
        // COURSE ACTIVE DU CHAUFFEUR
        // ============================================================

        [HttpGet("active/{driverId:int}")]
        public async Task<IActionResult> GetActiveRideForDriver(int driverId)
        {
            try
            {
                var ride = await _context.Rides
                    .Where(x =>
                        x.DriverId == driverId &&
                        x.Status == "Accepted")
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        RideId = x.Id,

                        DriverId = x.DriverId,

                        PickupLatitude = x.PickupLatitude,
                        PickupLongitude = x.PickupLongitude,

                        DestinationLatitude = x.DestinationLatitude,
                        DestinationLongitude = x.DestinationLongitude,

                        Departure = x.Departure,
                        Destination = x.Destination,

                        Price = x.Price,

                        RideType = x.RideType,

                        Passengers = x.CurrentPassengers,

                        DistanceToDriver = 0,

                        Status = x.Status
                    })
                    .FirstOrDefaultAsync();

                if (ride == null)
                {
                    return Ok(null);
                }

                Console.WriteLine(
                    $"COURSE ACTIVE DRIVER {driverId} : " +
                    $"RideId={ride.RideId}, Status={ride.Status}");

                return Ok(ride);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"ERREUR GET ACTIVE RIDE : {ex}");

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "Erreur lors de la récupération de la course active.",

                        error = ex.Message
                    });
            }
        }
}