namespace MoraTuk.API.Models
{
    public class DriverEarning
    {
        public int Id { get; set; }

        public int RideId { get; set; }

        public int DriverId { get; set; }

        public int PaymentId { get; set; }

        public int? DriverPayoutId { get; set; }

        public DriverPayout? DriverPayout { get; set; }

        // Montant total payé par le client
        public decimal GrossAmount { get; set; }

        // Commission ZOTRANAKA
        public decimal CommissionAmount { get; set; }

        // Frais d'attente conservés par ZOTRANAKA
        public decimal WaitingFeeAmount { get; set; }

        // Montant réellement dû au chauffeur
        public decimal DriverAmount { get; set; }

        // Pending / ReadyForPayout / Paid / Failed
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        public string? PayoutReference { get; set; }

        // Relations
        public Ride? Ride { get; set; }

        public Driver? Driver { get; set; }

        public Payment? Payment { get; set; }

    }
}