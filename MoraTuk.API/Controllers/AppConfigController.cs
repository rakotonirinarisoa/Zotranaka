using Microsoft.AspNetCore.Mvc;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/config")]
public class AppConfigController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            apiUrl = "https://annotation-depending-forward-low.trycloudflare.com",
            version = "1.0.0"
        });
    }
}