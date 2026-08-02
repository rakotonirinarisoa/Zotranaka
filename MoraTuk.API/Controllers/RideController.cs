using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Models;
using MoraTuk.API.Services;
using Microsoft.AspNetCore.SignalR;
using MoraTuk.API.Hubs;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RideController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DistanceService _distanceService;
    private readonly IHubContext<TrackingHub> _hub;

    public RideController(
    AppDbContext context,
    DistanceService distanceService,
    IHubContext<TrackingHub> hub)
    {
        _context = context;
        _distanceService = distanceService;
        _hub = hub;
    }
    private const decimal PRICE_PER_KM = 1500;
    private const decimal MIN_RIDE_PRICE = 2100;


    // Client commande une course
    [HttpPost("create")]
    public async Task<IActionResult> CreateRide(Ride ride)
    {
        var client = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == ride.ClientId);


        if (client == null) return BadRequest("Client introuvable");

        if (client.Role != "Client")return BadRequest("Cet utilisateur n'est pas un client");
       
        // Chauffeurs disponibles
        var drivers = await _context.Drivers
            .Include(d => d.User)
            .Where(d => d.IsAvailable)
            .ToListAsync();

        Console.WriteLine("=== APRES RECUPERATION DRIVERS ===");
        Console.WriteLine($"Nombre drivers : {drivers.Count}");
        foreach(var d in drivers)
        {
            Console.WriteLine(
                $"Disponible : DriverId={d.Id}, UserId={d.UserId}, Nom={d.User?.FullName}");
        }
        if (!drivers.Any())return BadRequest("Aucun chauffeur disponible.");
        Console.WriteLine("=== AVANT CALCUL DISTANCE ===");
        // Dernière position connue de chaque chauffeur
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
        if (nearestDriver != null)
            {
                Console.WriteLine(
                    $"Driver {nearestDriver.Driver.Id} - Distance : {nearestDriver.Distance} km");
            }
            else
            {
                Console.WriteLine("Aucun chauffeur trouvé");
            }
        if (nearestDriver != null)
        {
                Console.WriteLine(
                $"Driver {nearestDriver.Driver.Id} - Distance : {nearestDriver.Distance} km");
        }
        else if (nearestDriver.Distance > 5)
        {
            return BadRequest(
                "Aucun chauffeur disponible dans un rayon de 5 km.");
        }
        else
        {
                return BadRequest(
            "Aucun chauffeur avec une position connue.");
        }
        Console.WriteLine("=== APRES CALCUL DISTANCE ===");
        ride.DriverId = nearestDriver.Driver.Id;
        ride.MaxPassengers = 4;

        ride.CurrentPassengers = 1;
        ride.RideType ??= "Shared";
        ride.Status = "WaitingDriver";
        ride.CreatedAt = DateTime.UtcNow;
        // Prix provisoire
        var distance = _distanceService.Calculate(
            ride.PickupLatitude,
            ride.PickupLongitude,
            ride.DestinationLatitude,
            ride.DestinationLongitude);
        // Tarif : 2100 Ar / km
       decimal price = Math.Ceiling(
            (decimal)(distance * (double)PRICE_PER_KM) / 100
        ) * 100;

        // Minimum 2100 Ar
        if(price < MIN_RIDE_PRICE)
        {
            price = MIN_RIDE_PRICE;
        }
        decimal finalPrice;


        if(ride.RideType == "Private")
        {
            // Tuk-Tuk complet = 4 places
            finalPrice = price * 4;
        }
        else
        {
            // Une place seulement
            finalPrice = price;
        }
        ride.Price = finalPrice;
        // Statut initial
        //ride.Status = "Pending";

        ride.CreatedAt = DateTime.UtcNow;


        _context.Rides.Add(ride);

        await _context.SaveChangesAsync();

        Console.WriteLine(
            $"Envoi NewRide vers driver-{nearestDriver.Driver.Id}");
       
       await _hub.Clients
            .Group($"driver-{nearestDriver.Driver.Id}")
            .SendAsync(
                "NewRide",
                new
                {
                    RideId = ride.Id,

                    PickupLatitude = ride.PickupLatitude,
                    PickupLongitude = ride.PickupLongitude,

                    DestinationLatitude = ride.DestinationLatitude,
                    DestinationLongitude = ride.DestinationLongitude,

                    Price = ride.Price,

                    RideType = ride.RideType,
                    Passengers = ride.CurrentPassengers,

                    DistanceToDriver = nearestDriver.Distance
                });

        return Ok(new
        {
            Message="Course créée",

            RideId=ride.Id,

            DriverId=nearestDriver.Driver.Id,

            Driver=nearestDriver.Driver.User?.FullName,

            DistanceKm=Math.Round(
                nearestDriver.Distance,2),

            Price=ride.Price,

            RideType=ride.RideType,

            Passengers=
            $"{ride.CurrentPassengers}/{ride.MaxPassengers}",

            Status=ride.Status
        });
    }
    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptRide(
        int id,
        int driverId)
    {
        var ride = await _context.Rides
            .FirstOrDefaultAsync(x => x.Id == id);


        Console.WriteLine($"Ride trouvée: {ride?.Id}");
        Console.WriteLine($"Status: {ride?.Status}");
        if (ride == null)
        {
            return NotFound("Course introuvable");
        }


        if (ride.Status != "WaitingDriver")
        {
            return BadRequest("Cette course n'est plus disponible");
        }


        var driver = await _context.Drivers
            .FirstOrDefaultAsync(x => x.Id == driverId);

         if (driver == null)
        {
            return BadRequest("Chauffeur introuvable");
        }

        // Vérifier disponibilité
        if(!driver.IsAvailable)
        {
            return BadRequest(
                "Ce chauffeur est déjà occupé");
        }

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
                    driverId = driverId,
                    status = "Accepted"
                });

        return Ok(new
        {
            message = "Course acceptée",
            rideId = ride.Id,
            driverId = driverId,
            status = ride.Status
        });
    }
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableRides()
    {
        var rides = await _context.Rides
            .Where(x => 
                x.Status == "WaitingDriver" &&
                x.DriverId != null)
            .Include(x => x.Client)
            .ToListAsync();


        return Ok(rides);
    }
    [HttpPost("estimate")]
    public IActionResult EstimatePrice(Ride ride)
    {
        var distance = _distanceService.Calculate(
            ride.PickupLatitude,
            ride.PickupLongitude,
            ride.DestinationLatitude,
            ride.DestinationLongitude);

        // prix d'une place
        decimal price = Math.Ceiling(
            (decimal)(distance * 1500) / 100
        ) * 100;


        if(price < 2100)
        {
            price = 2100;
        }

        // si Tuk-Tuk complet
        if(ride.RideType == "Private")
        {
            price = price * 4;
        }
        return Ok(new
        {
            DistanceKm = Math.Round(distance,2),
            Price = price
        });
    }
}