using MoraTuk.API.Models;

namespace MoraTuk.API.Services
{
    public interface IMvolaService
    {
        Task<string> MerchantPayAsync(
            MvolaPaymentRequest request);
        Task<string> GetPaymentStatusAsync(
            string serverCorrelationId);
        Task<string> GetTransactionAsync(
            string transactionReference);
        Task<string> TransferToDriverAsync(
            string driverMvolaNumber,
            decimal amount,
            string description,
            string reference);
    }
}
