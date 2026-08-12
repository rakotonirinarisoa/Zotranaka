namespace MoraTuk.API.Models;

public class DriverPayout
{
    public int Id { get; set; }

    public int DriverId { get; set; }

    // Date de la journée concernée
    public DateTime PayoutDate { get; set; }

    // Nombre de courses regroupées
    public int TotalRides { get; set; }

    // Total payé par les clients
    public decimal GrossAmount { get; set; }

    // Commission ZOTRANAKA
    public decimal CommissionAmount { get; set; }

    // Frais d'attente conservés par ZOTRANAKA
    public decimal WaitingFeeAmount { get; set; }

    // Montant envoyé au chauffeur
    public decimal DriverAmount { get; set; }

    // Pending / Processing / Paid / Failed
    public string Status { get; set; } = "Pending";

    // Référence du transfert MVola
    public string? TransactionReference { get; set; }

    // serverCorrelationId du transfert MVola
    public string? ServerCorrelationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public string? FailureReason { get; set; }

    // Relation
    public Driver? Driver { get; set; }

    public List<DriverEarning> Earnings { get; set; } = new();
}