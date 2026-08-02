using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.DTOs;
using MoraTuk.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MoraTuk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
    AppDbContext context,
    IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var userExist = await _context.Users
            .AnyAsync(x => x.Phone == dto.Phone);

        if (userExist)
        {
            return BadRequest("Ce numéro existe déjà");
        }


        var user = new User
        {
            FullName = dto.FullName,
            Phone = dto.Phone,

            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),

            Role = dto.Role
        };


        _context.Users.Add(user);

        await _context.SaveChangesAsync();


        return Ok(new
        {
            message = "Compte créé avec succès",
            user.Id,
            user.FullName,
            user.Role
        });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Phone == dto.Phone);

        if (user == null)
        {
            return Unauthorized("Utilisateur introuvable");
        }


        bool passwordValid = BCrypt.Net.BCrypt.Verify(
            dto.Password,
            user.PasswordHash
        );


        if (!passwordValid)
        {
            return Unauthorized("Mot de passe incorrect");
        }


        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("role", user.Role),
            new Claim("phone", user.Phone)
        };


        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!
            )
        );


        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );


        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: credentials
        );


        return Ok(new
        {
            token = new JwtSecurityTokenHandler()
                .WriteToken(token),

            user = new
            {
                user.Id,
                user.FullName,
                user.Role
            }
        });
    }
}
