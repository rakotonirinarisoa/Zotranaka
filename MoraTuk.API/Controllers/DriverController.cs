using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Models;
using MoraTuk.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using MoraTuk.API.Services;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriverController : ControllerBase
{
    private readonly AppDbContext _context;
    //private readonly IHubContext<RideHub> _hub;
    private readonly IHubContext<TrackingHub> _hub;

    public DriverController(AppDbContext context,
    IHubContext<TrackingHub> hub)
    {
        _context = context;
         _hub = hub;
    }


    [HttpPost("create")]
    public async Task<IActionResult> CreateDriver(Driver driver)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == driver.UserId);

        if (user == null)
        {
            return BadRequest("Utilisateur introuvable");
        }


        if (user.Role != "Driver")
        {
            return BadRequest("Cet utilisateur n'est pas un chauffeur");
        }


        var exists = await _context.Drivers
            .AnyAsync(x => x.UserId == driver.UserId);

        if (exists)
        {
            return BadRequest("Profil chauffeur existe déjà");
        }


        driver.LastUpdate = DateTime.UtcNow;

        _context.Drivers.Add(driver);

        await _context.SaveChangesAsync();


        return Ok(driver);
    }
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        bool isAvailable)
    {
        var driver = await _context.Drivers
            .FindAsync(id);

        if (driver == null)
        {
            return NotFound("Chauffeur introuvable");
        }


        driver.IsAvailable = isAvailable;
        driver.LastUpdate = DateTime.UtcNow;


        await _context.SaveChangesAsync();


        return Ok(new
        {
            message = "Statut mis à jour",
            driver.Id,
            driver.IsAvailable
        });
    }
    [HttpPut("{id}/location")]
    public async Task<IActionResult> UpdateLocation(
        int id,
        double latitude,
        double longitude)
    {
        var driver = await _context.Drivers
            .FindAsync(id);


        if (driver == null)
        {
            return NotFound("Chauffeur introuvable");
        }


        driver.Latitude = latitude;
        driver.Longitude = longitude;
        driver.LastUpdate = DateTime.UtcNow;


        await _context.SaveChangesAsync();

        // Chercher la course active
    var ride = await _context.Rides
        .FirstOrDefaultAsync(x =>
            x.DriverId == id &&
            x.Status == "Accepted");


    if(ride != null)
    {
        await _hub.Clients
            .Group($"client-{ride.ClientId}")
            .SendAsync(
                "DriverLocation",
                new
                {
                    latitude,
                    longitude
                });
    }

        return Ok(new
        {
            message = "Position GPS mise à jour",
            driver.Id,
            driver.Latitude,
            driver.Longitude,
            driver.LastUpdate
        });
    }
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyDrivers(
        double latitude,
        double longitude,
        double radiusKm = 5)
    {
        var drivers = await _context.Drivers
            .Include(x => x.User)
            .Where(x => x.IsAvailable)
            .ToListAsync();


        var result = drivers
            .Select(driver => new
            {
                driver.Id,
                Name = driver.User.FullName,
                driver.VehicleNumber,

                DistanceKm = CalculateDistance(
                    latitude,
                    longitude,
                    driver.Latitude,
                    driver.Longitude)
            })
            .Where(x => x.DistanceKm <= radiusKm)
            .OrderBy(x => x.DistanceKm)
            .ToList();


        return Ok(result);
    }
    [HttpGet("by-user/{userId}")]
    public async Task<IActionResult> GetDriverByUser(int userId)
    {
        var driver = await _context.Drivers
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (driver == null)
        {
            return NotFound("Profil chauffeur introuvable");
        }

        return Ok(new
        {
            driverId = driver.Id,
            userId = driver.UserId
        });
    }
    [HttpPost("{id}/sync-aika")]
public async Task<IActionResult> SyncAika(
    int id,
    [FromServices] AikaLocationService aikaService)
{
    var driver = await _context.Drivers
        .FindAsync(id);

    if (driver == null)
    {
        return NotFound(
            "Chauffeur introuvable");
    }

    // ============================================================
    // VERIFICATION GPS AIKA
    // ============================================================

    if (driver.AikaDeviceId == null)
    {
        return BadRequest(
            "Aucun GPS AIKA associé à ce chauffeur.");
    }

    if (string.IsNullOrWhiteSpace(
            driver.AikaUsername))
    {
        return BadRequest(
            "Username AIKA manquant.");
    }

    if (string.IsNullOrWhiteSpace(
            driver.AikaPassword))
    {
        return BadRequest(
            "Password AIKA manquant.");
    }

    try
    {
        // ========================================================
        // AIKA -> GetTracking
        // ========================================================

        var location =
            await aikaService.GetTrackingAsync(
                driver.AikaDeviceId.Value,
                driver.AikaUsername,
                driver.AikaPassword);

        if (location == null)
        {
            return BadRequest(
                "AIKA n'a pas retourné de position GPS valide.");
        }

        // ========================================================
        // MISE A JOUR DATABASE
        // ========================================================

        driver.Latitude =
            location.Latitude;

        driver.Longitude =
            location.Longitude;

        driver.LastUpdate =
            DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // ========================================================
        // COURSE ACTIVE
        // ========================================================

        var ride =
            await _context.Rides
                .FirstOrDefaultAsync(x =>
                    x.DriverId == id &&
                    x.Status == "Accepted");

        if (ride != null)
        {
            await _hub.Clients
                .Group($"client-{ride.ClientId}")
                .SendAsync(
                    "DriverLocation",
                    new
                    {
                        latitude =
                            location.Latitude,

                        longitude =
                            location.Longitude
                    });
        }

        // ========================================================
        // REPONSE
        // ========================================================

        return Ok(new
        {
            success = true,

            message =
                "Position AIKA synchronisée",

            driverId =
                driver.Id,

            vehicleNumber =
                driver.VehicleNumber,

            aikaDeviceId =
                driver.AikaDeviceId,

            latitude =
                location.Latitude,

            longitude =
                location.Longitude,

            speed =
                location.Speed,

            course =
                location.Course,

            positionTime =
                location.PositionTime,

            gps =
                location.IsGps,

            stopped =
                location.IsStopped,

            battery =
                location.Battery,

            status =
                location.Status,

            lastUpdate =
                driver.LastUpdate
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"AIKA SYNC ERROR DRIVER {id}: {ex}");

        return StatusCode(
            500,
            new
            {
                success = false,

                message =
                    "Erreur lors de la synchronisation AIKA.",

                error =
                    ex.Message
            });
    }
}

    [HttpGet("fleet-locations")]
    public async Task<IActionResult> GetFleetLocations()
    {
        var drivers = await _context.Drivers
            .Where(x => x.Latitude != 0 && x.Longitude != 0)
            .Select(x => new
            {
                driverId = x.Id,
                vehicleNumber = x.VehicleNumber,
                latitude = x.Latitude,
                longitude = x.Longitude,
                isAvailable = x.IsAvailable,
                lastUpdate = x.LastUpdate,
                speed = AikaTrackingWorker.GetDriverSpeed(x.Id)
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            total = drivers.Count,
            drivers
        });
    }

    [HttpPost("aika-device-info")]
    public async Task<IActionResult> GetAikaDeviceInfo(
        string username,
        string password,
        [FromServices] AikaLocationService aikaService)
    {
        try
        {
            var device =
                await aikaService.LoginAndGetDeviceInfoAsync(
                    username,
                    password);

            if (device == null)
            {
                return BadRequest(
                    "AIKA n'a retourné aucun appareil.");
            }

            return Ok(new
            {
                success = true,

                deviceId =
                    device.DeviceId,

                deviceName =
                    device.DeviceName,

                model =
                    device.Model,

                serialNumber =
                    device.SerialNumber,

                imei =
                    device.Imei,

                key =
                    device.Key
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }
    
        private double CalculateDistance(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double R = 6371;


        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;


        var a =
            Math.Sin(dLat / 2) *
            Math.Sin(dLat / 2) +

            Math.Cos(lat1 * Math.PI / 180) *
            Math.Cos(lat2 * Math.PI / 180) *

            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);


        var c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));


        return R * c;
    }
}
