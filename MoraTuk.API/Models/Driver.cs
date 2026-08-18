using System.ComponentModel.DataAnnotations;

namespace MoraTuk.API.Models;

public class Driver
{
    public int Id { get; set; }


    // Relation avec User
    public int UserId { get; set; }

    public User? User { get; set; }


    // Informations Tuk-Tuk
    public string VehicleNumber { get; set; } = string.Empty;


    // Disponibilité
    public bool IsAvailable { get; set; } = false;


    // Position GPS
    public double Latitude { get; set; }

    public double Longitude { get; set; }


    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

    // Numéro MVola du chauffeur
    public string? MvolaNumber { get; set; }

    public int? AikaDeviceId { get; set; }

    public string? AikaSerialNumber { get; set; }


    public string? AikaUsername { get; set; }

    public string? AikaPassword { get; set; }
}
