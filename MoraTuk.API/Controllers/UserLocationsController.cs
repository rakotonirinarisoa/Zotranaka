using Microsoft.AspNetCore.Mvc;
using MoraTuk.API.Data;
using MoraTuk.API.Models;
using Microsoft.EntityFrameworkCore;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserLocationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserLocationsController(AppDbContext context)
    {
        _context = context;
    }


    [HttpPost]
    public async Task<IActionResult> Save(
        UserLocation location)
    {
        location.CreatedAt = DateTime.Now;

        _context.UserLocations.Add(location);

        await _context.SaveChangesAsync();

        return Ok(location);
    }
    [HttpGet("last/{userId}")]
    public async Task<IActionResult> GetLast(int userId)
    {
        var location = await _context.UserLocations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();


        if(location == null)
        {
            return NotFound();
        }


        return Ok(location);
    }
}