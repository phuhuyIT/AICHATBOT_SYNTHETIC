using WebApplication1.DTO;
using WebApplication1.DTO.Transaction;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface ITransactionService : IWriteService<TransactionCreateDTO, TransactionUpdateDTO, TransactionResponseDTO>, IReadService<TransactionResponseDTO>
    {
        Task<ServiceResult<IEnumerable<TransactionResponseDTO>>> GetTransactionsByUserIdAsync(string userId);
        Task<ServiceResult<IEnumerable<TransactionResponseDTO>>> GetTransactionHistoryAsync(TransactionHistoryFilterDTO filter);
        Task<ServiceResult<decimal>> GetUserTotalRechargeAmountAsync(string userId);
        Task<ServiceResult<TransactionResponseDTO>> GetLastTransactionByUserAsync(string userId);
        Task<ServiceResult<bool>> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status);
    }

    public interface IRechargeService
    {
        Task<ServiceResult<RechargeResponseDTO>> InitiateRechargeAsync(RechargeRequestDTO request);
        Task<ServiceResult<bool>> ProcessRechargeCallbackAsync(string paymentIntentId, string status, string? failureReason = null);
        Task<ServiceResult<TransactionResponseDTO>> ConfirmRechargeAsync(Guid transactionId);
        Task<ServiceResult<bool>> CancelRechargeAsync(Guid transactionId, string reason);
        Task<ServiceResult<IEnumerable<RechargePackage>>> GetActiveRechargePackagesAsync();
        Task<ServiceResult<IEnumerable<RechargePackage>>> GetPromotionalPackagesAsync();
    }

    public interface IPaymentService
    {
        Task<ServiceResult<string>> CreatePaymentIntentAsync(decimal amount, string currency, string userId, PaymentMethod paymentMethod);
        Task<ServiceResult<bool>> ProcessPaymentWebhookAsync(string paymentIntentId, string status, string gatewayResponse);
        Task<ServiceResult<PaymentTransaction>> GetPaymentByIntentIdAsync(string paymentIntentId);
    }
}
