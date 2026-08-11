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
    }
}
