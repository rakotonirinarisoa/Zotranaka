using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Models;
using MoraTuk.API.Services;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriverLocationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DistanceService _distanceService;

    public DriverLocationsController(AppDbContext context,DistanceService distanceService)
    {
        _context = context;
        _distanceService = distanceService;
    }

    [HttpPost]
    public async Task<IActionResult> Save(DriverLocation location)
    {
        location.CreatedAt = DateTime.Now;

        _context.DriverLocations.Add(location);

        await _context.SaveChangesAsync();

        return Ok(location);
    }

    [HttpGet("last/{driverId}")]
    public async Task<IActionResult> GetLast(int driverId)
    {
        var location = await _context.DriverLocations
            .Where(x => x.DriverId == driverId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (location == null)
            return NotFound();

        return Ok(location);
    }
    [HttpGet("nearest-driver")]
    public async Task<IActionResult> GetNearestDriver(int clientId)
    {
        var clientLocation = await _context.UserLocations
            .Where(x => x.UserId == clientId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
        if(clientLocation == null)
        {
            return NotFound("Position du client introuvable.");
        }
        var driverLocations = await _context.DriverLocations
            .GroupBy(x => x.DriverId)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .ToListAsync();
        var nearestDriver = driverLocations
            .Select(d => new
            {
                Driver = d,

                Distance = _distanceService.Calculate(
                    clientLocation.Latitude,
                    clientLocation.Longitude,
                    d.Latitude,
                    d.Longitude)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();
        if(nearestDriver == null)
        {
            return NotFound("Aucun chauffeur disponible.");
        }

        return Ok(new
        {
            driverId = nearestDriver.Driver.DriverId,
            latitude = nearestDriver.Driver.Latitude,
            longitude = nearestDriver.Driver.Longitude,
            distance = Math.Round(nearestDriver.Distance, 2)
        });
    }
}