using Microsoft.EntityFrameworkCore.Storage;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository.Interface
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<IEnumerable<Transaction>> GetTransactionsByUserIdAsync(string userId);
        Task<IEnumerable<Transaction>> GetTransactionsByUserIdAndTypeAsync(string userId, TransactionType type);
        Task<IEnumerable<Transaction>> GetTransactionsByStatusAsync(TransactionStatus status);
        Task<Transaction?> GetTransactionWithDetailsAsync(Guid transactionId);
        Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(string userId, DateTime? fromDate, DateTime? toDate, TransactionType? type, TransactionStatus? status, int pageNumber, int pageSize);
        Task<decimal> GetUserTotalRechargeAmountAsync(string userId);
        Task<Transaction?> GetLastTransactionByUserAsync(string userId);
        Task<bool> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status);
        Task<bool> UpdateUserBalanceAsync(string userId, decimal newBalance, decimal previousBalance, Guid transactionId);
        Task<bool> DeleteTransactionAsync(Guid transactionId);
        Task<IEnumerable<Transaction>> GetDeletedTransactionsAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }

    public interface IRechargePackageRepository : IGenericRepository<RechargePackage>
    {
        Task<IEnumerable<RechargePackage>> GetActivePackagesAsync();
        Task<IEnumerable<RechargePackage>> GetPromotionalPackagesAsync();
        Task<RechargePackage?> GetPackageWithTransactionsAsync(Guid packageId);
    }

    public interface IPaymentTransactionRepository : IGenericRepository<PaymentTransaction>
    {
        Task<PaymentTransaction?> GetByExternalTransactionIdAsync(string externalTransactionId);
        Task<PaymentTransaction?> GetByPaymentIntentIdAsync(string paymentIntentId);
        Task<IEnumerable<PaymentTransaction>> GetPaymentTransactionsByTransactionIdAsync(Guid transactionId);
        Task<bool> UpdatePaymentStatusAsync(Guid paymentTransactionId, TransactionStatus status, string? failureReason = null);
    }
}
