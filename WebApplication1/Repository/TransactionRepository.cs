using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByUserIdAsync(string userId)
        {
            return await _context.Set<Transaction>()
                .Where(t => t.UserId == userId && t.IsActive)
                .Include(t => t.RechargePackage)
                .Include(t => t.PaymentTransactions)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByUserIdAndTypeAsync(string userId, TransactionType type)
        {
            return await _context.Set<Transaction>()
                .Where(t => t.UserId == userId && t.TransactionType == type && t.IsActive)
                .Include(t => t.RechargePackage)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByStatusAsync(TransactionStatus status)
        {
            return await _context.Set<Transaction>()
                .Where(t => t.Status == status && t.IsActive)
                .Include(t => t.User)
                .Include(t => t.RechargePackage)
                .Include(t => t.PaymentTransactions)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<Transaction?> GetTransactionWithDetailsAsync(Guid transactionId)
        {
            return await _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.RechargePackage)
                .Include(t => t.PaymentTransactions)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionHistoryAsync(string userId, DateTime? fromDate, DateTime? toDate, TransactionType? type, TransactionStatus? status, int pageNumber, int pageSize)
        {
            var query = _context.Set<Transaction>()
                .Where(t => t.UserId == userId && t.IsActive);

            if (fromDate.HasValue)
                query = query.Where(t => t.TransactionDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.TransactionDate <= toDate.Value);

            if (type.HasValue)
                query = query.Where(t => t.TransactionType == type.Value);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            return await query
                .Include(t => t.RechargePackage)
                .OrderByDescending(t => t.TransactionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<decimal> GetUserTotalRechargeAmountAsync(string userId)
        {
            return await _context.Set<Transaction>()
                .Where(t => t.UserId == userId && 
                           t.TransactionType == TransactionType.Recharge && 
                           t.Status == TransactionStatus.Completed && 
                           t.IsActive)
                .SumAsync(t => t.Amount);
        }

        public async Task<Transaction?> GetLastTransactionByUserAsync(string userId)
        {
            return await _context.Set<Transaction>()
                .Where(t => t.UserId == userId && t.IsActive)
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status)
        {
            var transaction = await _context.Set<Transaction>()
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null) return false;

            transaction.Status = status;
            transaction.UpdatedAt = DateTime.UtcNow;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateUserBalanceAsync(string userId, decimal newBalance, decimal previousBalance, Guid transactionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update user balance
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return false;

                user.Balance = newBalance;
                user.UpdatedAt = DateTime.UtcNow;

                // Update transaction balance fields
                var transactionRecord = await _context.Set<Transaction>()
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
                
                if (transactionRecord != null)
                {
                    transactionRecord.BalanceBefore = previousBalance;
                    transactionRecord.BalanceAfter = newBalance;
                    transactionRecord.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // Override AddAsync to ensure proper transaction ID generation
        public override async Task<Transaction> AddAsync(Transaction entity)
        {
            if (entity.TransactionId == Guid.Empty)
            {
                entity.TransactionId = Guid.NewGuid();
            }
            
            await _context.Set<Transaction>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // Add missing method implementation
        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        // Add missing DeleteTransactionAsync method
        public async Task<bool> DeleteTransactionAsync(Guid transactionId)
        {
            var transaction = await _context.Set<Transaction>()
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
            
            if (transaction == null) return false;
            
            _context.Set<Transaction>().Remove(transaction);
            return await _context.SaveChangesAsync() > 0;
        }

        // Add missing GetDeletedTransactionsAsync method
        public async Task<IEnumerable<Transaction>> GetDeletedTransactionsAsync()
        {
            return await _context.Set<Transaction>()
                .Where(t => t.IsActive == false)
                .Include(t => t.User)
                .Include(t => t.RechargePackage)
                .Include(t => t.PaymentTransactions)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }
    }

    public class RechargePackageRepository : GenericRepository<RechargePackage>, IRechargePackageRepository
    {
        public RechargePackageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<RechargePackage>> GetActivePackagesAsync()
        {
            return await _context.Set<RechargePackage>()
                .Where(p => p.IsActive )
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Amount)
                .ToListAsync();
        }

        public async Task<IEnumerable<RechargePackage>> GetPromotionalPackagesAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Set<RechargePackage>()
                .Where(p => p.IsActive && 
                           p.IsPromotional && 
                           (!p.PromotionStartDate.HasValue || p.PromotionStartDate <= now) &&
                           (!p.PromotionEndDate.HasValue || p.PromotionEndDate >= now))
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
        }

        public async Task<RechargePackage?> GetPackageWithTransactionsAsync(Guid packageId)
        {
            return await _context.Set<RechargePackage>()
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.PackageId == packageId);
        }
    }

    public class PaymentTransactionRepository : GenericRepository<PaymentTransaction>, IPaymentTransactionRepository
    {
        public PaymentTransactionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<PaymentTransaction?> GetByExternalTransactionIdAsync(string externalTransactionId)
        {
            return await _context.Set<PaymentTransaction>()
                .Include(pt => pt.Transaction)
                .FirstOrDefaultAsync(pt => pt.ExternalTransactionId == externalTransactionId);
        }

        public async Task<PaymentTransaction?> GetByPaymentIntentIdAsync(string paymentIntentId)
        {
            return await _context.Set<PaymentTransaction>()
                .Include(pt => pt.Transaction)
                .FirstOrDefaultAsync(pt => pt.PaymentIntentId == paymentIntentId);
        }

        public async Task<IEnumerable<PaymentTransaction>> GetPaymentTransactionsByTransactionIdAsync(Guid transactionId)
        {
            return await _context.Set<PaymentTransaction>()
                .Where(pt => pt.TransactionId == transactionId)
                .OrderByDescending(pt => pt.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdatePaymentStatusAsync(Guid paymentTransactionId, TransactionStatus status, string? failureReason = null)
        {
            var paymentTransaction = await _context.Set<PaymentTransaction>()
                .FirstOrDefaultAsync(pt => pt.PaymentTransactionId == paymentTransactionId);

            if (paymentTransaction == null) return false;

            paymentTransaction.Status = status;
            paymentTransaction.UpdatedAt = DateTime.UtcNow;

            if (status == TransactionStatus.Completed)
                paymentTransaction.CompletedAt = DateTime.UtcNow;
            else if (status == TransactionStatus.Failed)
            {
                paymentTransaction.FailedAt = DateTime.UtcNow;
                paymentTransaction.FailureReason = failureReason;
            }

            return await _context.SaveChangesAsync() > 0;
        }
    }
}
