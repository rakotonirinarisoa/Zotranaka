using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
namespace MoraTuk.API.Controllers;
using MoraTuk.API.Services;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DistanceService _distanceService;

    public LocationsController(AppDbContext context,DistanceService distanceService)
    {
        _context = context;
        _distanceService = distanceService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Ok(new List<Location>());

        var locations = await _context.Locations
            .Where(x => x.Name.Contains(text))
            .OrderBy(x => x.Name)
            .Take(20)
            .ToListAsync();

        return Ok(locations);
    }
    [HttpGet("nearest")]
    public async Task<IActionResult> Nearest(
        double latitude,
        double longitude)
    {

        var locations = await _context.Locations
            .ToListAsync();


        if (!locations.Any())
        {
            return NotFound("Aucun lieu trouvé");
        }


        var nearest = locations
            .Select(x => new
            {
                Location = x,

                Distance = _distanceService.Calculate(
                    latitude,
                    longitude,
                    x.Latitude,
                    x.Longitude)
            })
            //.Where(x => x.Distance < 0.1)
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        // return Ok(new
        // {
        //     name = nearest.Location.Name,

        //     category = nearest.Location.Category,

        //     distanceKm = Math.Round(
        //         nearest.Distance,2)
        // });
        return Ok(new
        {
            location = nearest.Location,
            distance = nearest.Distance
        });
    }
}