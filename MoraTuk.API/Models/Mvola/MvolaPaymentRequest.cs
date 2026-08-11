namespace MoraTuk.API.Models
{
    public class MvolaPaymentRequest
    {
        public int RideId { get; set; }
        
        public string Amount { get; set; } = string.Empty;

        public string Currency { get; set; } = "Ar";

        public string DescriptionText { get; set; } = string.Empty;

        public string RequestingOrganisationTransactionReference { get; set; } = string.Empty;

        public string RequestDate { get; set; } = string.Empty;

        public string? OriginalTransactionReference { get; set; }

        // Client qui paie
        public List<MvolaParty> DebitParty { get; set; } = new();

        // Compte marchand MoraTUK qui reçoit
        public List<MvolaParty> CreditParty { get; set; } = new();

        public List<MvolaMetadata> Metadata { get; set; } = new();
    }

    // ============================================================
    // MVOLA PARTY
    // ============================================================

    public class MvolaParty
    {
        public string Key { get; set; } = "msisdn";

        public string Value { get; set; } = string.Empty;
    }

    // ============================================================
    // MVOLA METADATA
    // ============================================================

    public class MvolaMetadata
    {
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}