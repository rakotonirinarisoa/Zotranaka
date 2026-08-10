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

    private const decimal PRICE_PER_KM = 1500m;
    private const decimal MIN_RIDE_PRICE = 2100m;

    public RideController(
        AppDbContext context,
        DistanceService distanceService,
        IHubContext<TrackingHub> hub)
    {
        _context = context;
        _distanceService = distanceService;
        _hub = hub;
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
        [FromBody] Ride ride)
    {
        try
        {
            if (ride == null)
            {
                return BadRequest(
                    "Les données de la course sont obligatoires.");
            }

            var client = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Id == ride.ClientId);

            if (client == null)
                return BadRequest("Client introuvable.");

            if (client.Role != "Client")
                return BadRequest(
                    "Cet utilisateur n'est pas un client.");

            // ----------------------------------------------------
            // CHAUFFEURS DISPONIBLES
            // ----------------------------------------------------

            var drivers = await _context.Drivers
                .Include(d => d.User)
                .Where(d => d.IsAvailable)
                .ToListAsync();

            Console.WriteLine(
                $"Nombre drivers disponibles : {drivers.Count}");

            if (!drivers.Any())
            {
                return BadRequest(
                    "Aucun chauffeur disponible.");
            }

            // ----------------------------------------------------
            // DERNIÈRE POSITION DES CHAUFFEURS
            // ----------------------------------------------------

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
                            ride.PickupLatitude,
                            ride.PickupLongitude,
                            location.Latitude,
                            location.Longitude)
                    })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (nearestDriver == null)
            {
                return BadRequest(
                    "Aucun chauffeur avec une position connue.");
            }

            Console.WriteLine(
                $"Driver {nearestDriver.Driver.Id} - " +
                $"Distance : {nearestDriver.Distance} km");

            if (nearestDriver.Distance > 5)
            {
                return BadRequest(
                    "Aucun chauffeur disponible dans un rayon de 5 km.");
            }

            // ----------------------------------------------------
            // AFFECTATION
            // ----------------------------------------------------

            ride.DriverId = nearestDriver.Driver.Id;

            ride.MaxPassengers = 4;
            ride.CurrentPassengers = 1;

            ride.RideType ??= "Shared";

            ride.Status = "WaitingDriver";

            ride.CreatedAt = DateTime.UtcNow;

            // ----------------------------------------------------
            // DISTANCE COURSE
            // ----------------------------------------------------

            var distance = _distanceService.Calculate(
                ride.PickupLatitude,
                ride.PickupLongitude,
                ride.DestinationLatitude,
                ride.DestinationLongitude);

            // ----------------------------------------------------
            // PRIX
            // ----------------------------------------------------

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

            ride.Price = price;

            _context.Rides.Add(ride);

            await _context.SaveChangesAsync();

            Console.WriteLine(
                $"Course créée : {ride.Id}");

            // ----------------------------------------------------
            // NOTIFICATION SIGNALR
            // ----------------------------------------------------

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

                        Price = ride.Price,

                        RideType = ride.RideType,

                        Passengers =
                            ride.CurrentPassengers,

                        DistanceToDriver =
                            nearestDriver.Distance,

                        Departure =
                            ride.Departure,

                        Destination =
                            ride.Destination
                    });

            return Ok(new
            {
                Message = "Course créée",

                RideId = ride.Id,

                DriverId =
                    nearestDriver.Driver.Id,

                Driver =
                    nearestDriver.Driver.User?.FullName,

                DistanceKm =
                    Math.Round(
                        nearestDriver.Distance,
                        2),

                Price = ride.Price,

                RideType =
                    ride.RideType,

                Passengers =
                    $"{ride.CurrentPassengers}/{ride.MaxPassengers}",

                Status =
                    ride.Status
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
                    message =
                        "Erreur lors de la création de la course.",

                    error =
                        ex.Message
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
        var ride = await _context.Rides
            .FirstOrDefaultAsync(
                x => x.Id == id);

        if (ride == null)
            return NotFound(
                "Course introuvable.");

        if (ride.Status != "WaitingDriver")
            return BadRequest(
                "Cette course n'est plus disponible.");

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
    // TOUTES LES COURSES EN ATTENTE
    // ============================================================

   [HttpGet("available/{driverId:int}")]
public async Task<IActionResult> GetAvailableRidesForDriver(int driverId)
{
    try
    {
        var rides = await _context.Rides
            .Where(x =>
                x.Status == "WaitingDriver" &&
                x.DriverId == driverId)
            .Include(x => x.Client)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                RideId = x.Id,

                PickupLatitude = x.PickupLatitude,
                PickupLongitude = x.PickupLongitude,

                DestinationLatitude = x.DestinationLatitude,
                DestinationLongitude = x.DestinationLongitude,

                Price = x.Price,

                RideType = x.RideType,

                // IMPORTANT
                Passengers = x.CurrentPassengers,

                DistanceToDriver = 0,

                Departure = x.Departure,
                Destination = x.Destination,

                DriverId = x.DriverId,

                Status = x.Status
            })
            .ToListAsync();

        Console.WriteLine(
            $"COURSES POUR DRIVER {driverId} : {rides.Count}");

        return Ok(rides);
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"ERREUR GET AVAILABLE RIDES : {ex}");

        return StatusCode(
            500,
            new
            {
                message = "Erreur lors de la récupération des courses.",
                error = ex.Message
            });
    }
}
}