namespace MoraTuk.API.Models
{
    public class Payment
    {
        public int Id { get; set; }

        // Course concernée
        public int RideId { get; set; }

        public Ride? Ride { get; set; }

        // Montant
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "Ar";

        // MVola
        public string PaymentMethod { get; set; } = "MVola";

        public string? ServerCorrelationId { get; set; }

        public string? TransactionReference { get; set; }

        // Pending / Completed / Failed
        public string Status { get; set; } = "Pending";

        // Numéro du client
        public string? DebitMsisdn { get; set; }

        // Numéro marchand MoraTUK
        public string? CreditMsisdn { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}