using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Services;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DistanceService _distanceService;

    public LocationsController(
        AppDbContext context,
        DistanceService distanceService)
    {
        _context = context;
        _distanceService = distanceService;
    }


[HttpGet("nearest")]
public async Task<IActionResult> Nearest(
    double latitude,
    double longitude)
{
    try
    {
        Console.WriteLine("====================================");
        Console.WriteLine("RECHERCHE LIEU LE PLUS PROCHE");
        Console.WriteLine($"GPS : {latitude}, {longitude}");

        var locations =
            await _context.Locations
                .AsNoTracking()
                .ToListAsync();

        if (!locations.Any())
        {
            return NotFound(
                "Aucun lieu trouvé.");
        }

        var nearest =
            locations
                .Select(x => new
                {
                    Location = x,

                    Distance =
                        _distanceService.Calculate(
                            latitude,
                            longitude,
                            x.Latitude,
                            x.Longitude)
                })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

        if (nearest == null)
        {
            return NotFound(
                "Aucun lieu trouvé.");
        }

        Console.WriteLine(
            $"Lieu trouvé : {nearest.Location.Name}");

        Console.WriteLine(
            $"Distance : {nearest.Distance:F3} km");

        // =====================================================
        // MAXIMUM 500 MÈTRES
        // =====================================================

        if (nearest.Distance > 0.5)
        {
            Console.WriteLine(
                "Aucun lieu suffisamment proche.");

            return Ok(new
            {
                location = (object?)null,
                distance = nearest.Distance,
                message =
                    "Aucun lieu proche de votre position."
            });
        }

        Console.WriteLine(
            "Lieu accepté.");

        Console.WriteLine(
            "====================================");

        return Ok(new
        {
            location = nearest.Location,
            distance = Math.Round(
                nearest.Distance,
                3)
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"ERREUR NEAREST : {ex}");

        return StatusCode(
            500,
            new
            {
                message =
                    "Erreur lors de la recherche du lieu.",
                error = ex.Message
            });
    }
}

}