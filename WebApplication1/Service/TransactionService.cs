using Microsoft.AspNetCore.Identity;
using WebApplication1.DTO;
using WebApplication1.DTO.Transaction;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(ILogger<TransactionService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<TransactionResponseDTO>> CreateAsync(TransactionCreateDTO createDto)
        {
            return await ServiceResult<TransactionResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transaction = new Transaction
                {
                    UserId = createDto.UserId,
                    Amount = createDto.Amount,
                    TransactionType = (TransactionType)createDto.TransactionType,
                    RechargePackageId = createDto.RechargePackageId,
                    Description = createDto.Description,
                    ReferenceNumber = GenerateReferenceNumber(),
                    MetadataJson = createDto.MetadataJson,
                    Status = TransactionStatus.Pending,
                    TransactionDate = DateTime.UtcNow
                };

                await _unitOfWork.TransactionRepository.AddAsync(transaction);
                
                _logger.LogInformation("Transaction created successfully for user {UserId} with ID {TransactionId}", 
                    createDto.UserId, transaction.TransactionId);

                var responseDto = await MapToResponseDto(transaction);
                return ServiceResult<TransactionResponseDTO>.Success(responseDto, "Transaction created successfully");
            }, _unitOfWork, _logger, "Error creating transaction");
        }

        public async Task<ServiceResult<TransactionResponseDTO>> UpdateAsync(Guid id, TransactionUpdateDTO updateDto)
        {
            return await ServiceResult<TransactionResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var existingTransaction = await _unitOfWork.TransactionRepository.GetByIdAsync(id);
                if (existingTransaction == null)
                {
                    return ServiceResult<TransactionResponseDTO>.Failure("Transaction not found");
                }

                if (updateDto.Status.HasValue)
                    existingTransaction.Status = (TransactionStatus)updateDto.Status.Value;
                
                if (!string.IsNullOrEmpty(updateDto.Description))
                    existingTransaction.Description = updateDto.Description;
                
                if (!string.IsNullOrEmpty(updateDto.ReferenceNumber))
                    existingTransaction.ReferenceNumber = updateDto.ReferenceNumber;
                
                if (!string.IsNullOrEmpty(updateDto.MetadataJson))
                    existingTransaction.MetadataJson = updateDto.MetadataJson;

                existingTransaction.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.TransactionRepository.UpdateAsync(existingTransaction);

                _logger.LogInformation("Transaction {TransactionId} updated successfully", id);

                var responseDto = await MapToResponseDto(existingTransaction);
                return ServiceResult<TransactionResponseDTO>.Success(responseDto, "Transaction updated successfully");
            }, _unitOfWork, _logger, $"Error updating transaction {id}");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var success = await _unitOfWork.TransactionRepository.DeleteAsync(id);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("Transaction not found or could not be deleted");
                }

                _logger.LogInformation("Transaction {TransactionId} deleted successfully", id);
                return ServiceResult<bool>.Success(true, "Transaction deleted successfully");
            }, _unitOfWork, _logger, $"Error deleting transaction {id}");
        }

        public async Task<ServiceResult<bool>> SoftDeleteAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var success = await _unitOfWork.TransactionRepository.SoftDeleteAsync(id);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("Transaction not found or could not be soft deleted");
                }

                _logger.LogInformation("Transaction {TransactionId} soft deleted successfully", id);
                return ServiceResult<bool>.Success(true, "Transaction soft deleted successfully");
            }, _unitOfWork, _logger, $"Error soft deleting transaction {id}");
        }

        public async Task<ServiceResult<bool>> RestoreAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var success = await _unitOfWork.TransactionRepository.RestoreAsync(id);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("Transaction not found or could not be restored");
                }

                _logger.LogInformation("Transaction {TransactionId} restored successfully", id);
                return ServiceResult<bool>.Success(true, "Transaction restored successfully");
            }, _unitOfWork, _logger, $"Error restoring transaction {id}");
        }

        public async Task<ServiceResult<IEnumerable<TransactionResponseDTO>>> GetDeletedAsync()
        {
            return await ServiceResult<IEnumerable<TransactionResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transactions = await _unitOfWork.TransactionRepository.GetDeletedTransactionsAsync();
                var responseDtos = new List<TransactionResponseDTO>();
                
                foreach (var transaction in transactions)
                {
                    responseDtos.Add(await MapToResponseDto(transaction));
                }

                return ServiceResult<IEnumerable<TransactionResponseDTO>>.Success(responseDtos, "Deleted transactions retrieved successfully");
            }, _unitOfWork, _logger, "Error retrieving deleted transactions");
        }

        public async Task<ServiceResult<IEnumerable<TransactionResponseDTO>>> GetAllAsync()
        {
            return await ServiceResult<IEnumerable<TransactionResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transactions = await _unitOfWork.TransactionRepository.GetAllAsync();
                var responseDtos = new List<TransactionResponseDTO>();
                
                foreach (var transaction in transactions)
                {
                    responseDtos.Add(await MapToResponseDto(transaction));
                }

                return ServiceResult<IEnumerable<TransactionResponseDTO>>.Success(responseDtos, "Transactions retrieved successfully");
            }, _unitOfWork, _logger, "Error retrieving transactions");
        }

        public async Task<ServiceResult<TransactionResponseDTO>> GetByIdAsync(Guid id)
        {
            return await ServiceResult<TransactionResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transaction = await _unitOfWork.TransactionRepository.GetTransactionWithDetailsAsync(id);
                if (transaction == null)
                {
                    return ServiceResult<TransactionResponseDTO>.Failure("Transaction not found");
                }

                var responseDto = await MapToResponseDto(transaction);
                return ServiceResult<TransactionResponseDTO>.Success(responseDto, "Transaction retrieved successfully");
            }, _unitOfWork, _logger, $"Error retrieving transaction {id}");
        }

        public async Task<ServiceResult<IEnumerable<TransactionResponseDTO>>> GetTransactionsByUserIdAsync(string userId)
        {
            return await ServiceResult<IEnumerable<TransactionResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transactions = await _unitOfWork.TransactionRepository.GetTransactionsByUserIdAsync(userId);
                var responseDtos = new List<TransactionResponseDTO>();
                
                foreach (var transaction in transactions)
                {
                    responseDtos.Add(await MapToResponseDto(transaction));
                }

                _logger.LogInformation("Retrieved {Count} transactions for user {UserId}", responseDtos.Count, userId);
                return ServiceResult<IEnumerable<TransactionResponseDTO>>.Success(responseDtos, "User transactions retrieved successfully");
            }, _unitOfWork, _logger, $"Error retrieving transactions for user {userId}");
        }

        public async Task<ServiceResult<IEnumerable<TransactionResponseDTO>>> GetTransactionHistoryAsync(TransactionHistoryFilterDTO filter)
        {
            return await ServiceResult<IEnumerable<TransactionResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transactions = await _unitOfWork.TransactionRepository.GetTransactionHistoryAsync(
                    filter.UserId,
                    filter.FromDate,
                    filter.ToDate,
                    filter.TransactionType.HasValue ? (TransactionType)filter.TransactionType : null,
                    filter.Status.HasValue ? (TransactionStatus)filter.Status : null,
                    filter.PageNumber,
                    filter.PageSize
                );

                var responseDtos = new List<TransactionResponseDTO>();
                foreach (var transaction in transactions)
                {
                    responseDtos.Add(await MapToResponseDto(transaction));
                }

                return ServiceResult<IEnumerable<TransactionResponseDTO>>.Success(responseDtos, "Transaction history retrieved successfully");
            }, _unitOfWork, _logger, "Error retrieving transaction history");
        }

        public async Task<ServiceResult<decimal>> GetUserTotalRechargeAmountAsync(string userId)
        {
            return await ServiceResult<decimal>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var totalAmount = await _unitOfWork.TransactionRepository.GetUserTotalRechargeAmountAsync(userId);
                return ServiceResult<decimal>.Success(totalAmount, "Total recharge amount retrieved successfully");
            }, _unitOfWork, _logger, $"Error retrieving total recharge amount for user {userId}");
        }

        public async Task<ServiceResult<TransactionResponseDTO>> GetLastTransactionByUserAsync(string userId)
        {
            return await ServiceResult<TransactionResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transaction = await _unitOfWork.TransactionRepository.GetLastTransactionByUserAsync(userId);
                if (transaction == null)
                {
                    return ServiceResult<TransactionResponseDTO>.Failure("No transactions found for user");
                }

                var responseDto = await MapToResponseDto(transaction);
                return ServiceResult<TransactionResponseDTO>.Success(responseDto, "Last transaction retrieved successfully");
            }, _unitOfWork, _logger, $"Error retrieving last transaction for user {userId}");
        }

        public async Task<ServiceResult<bool>> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var success = await _unitOfWork.TransactionRepository.UpdateTransactionStatusAsync(transactionId, status);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("Transaction not found or status could not be updated");
                }

                _logger.LogInformation("Transaction {TransactionId} status updated to {Status}", transactionId, status);
                return ServiceResult<bool>.Success(true, "Transaction status updated successfully");
            }, _unitOfWork, _logger, $"Error updating transaction status {transactionId}");
        }

        private async Task<TransactionResponseDTO> MapToResponseDto(Transaction transaction)
        {
            string? rechargePackageName = null;
            if (transaction.RechargePackageId.HasValue && transaction.RechargePackage == null)
            {
                var package = await _unitOfWork.RechargePackageRepository.GetByIdAsync(transaction.RechargePackageId.Value);
                rechargePackageName = package?.Name;
            }
            else if (transaction.RechargePackage != null)
            {
                rechargePackageName = transaction.RechargePackage.Name;
            }

            return new TransactionResponseDTO
            {
                TransactionId = transaction.TransactionId,
                UserId = transaction.UserId,
                Amount = transaction.Amount,
                TransactionDate = transaction.TransactionDate,
                TransactionType = transaction.TransactionType.ToString(),
                Status = transaction.Status.ToString(),
                Description = transaction.Description,
                ReferenceNumber = transaction.ReferenceNumber,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                RechargePackageId = transaction.RechargePackageId,
                RechargePackageName = rechargePackageName,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            };
        }

        private string GenerateReferenceNumber()
        {
            return $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }
    }
}
