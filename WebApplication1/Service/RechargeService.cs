using Microsoft.AspNetCore.Identity;
using WebApplication1.DTO;
using WebApplication1.DTO.Transaction;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class RechargeService : IRechargeService
    {
        private readonly ILogger<RechargeService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly IPaymentService _paymentService;

        public RechargeService(
            ILogger<RechargeService> logger,
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            IPaymentService paymentService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _paymentService = paymentService;
        }

        public async Task<ServiceResult<RechargeResponseDTO>> InitiateRechargeAsync(RechargeRequestDTO request)
        {
            return await ServiceResult<RechargeResponseDTO>.ExecuteWithTransactionAsync(async () =>
            {
                // Validate user exists
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return ServiceResult<RechargeResponseDTO>.Failure("User not found");
                }

                // Add user status validation
                if (!user.IsActive)
                {
                    return ServiceResult<RechargeResponseDTO>.Failure("User account is inactive");
                }

                decimal rechargeAmount;
                Guid? packageId = null;

                // Determine recharge amount
                if (request.RechargePackageId.HasValue)
                {
                    var package = await _unitOfWork.RechargePackageRepository.GetByIdAsync(request.RechargePackageId.Value);
                    if (package == null || !package.IsActive)
                    {
                        return ServiceResult<RechargeResponseDTO>.Failure("Invalid recharge package");
                    }
                    rechargeAmount = package.TotalAmount; // Amount + Bonus
                    packageId = package.PackageId;
                }
                else if (request.CustomAmount.HasValue)
                {
                    // Validate custom amount
                    if (request.CustomAmount.Value <= 0)
                    {
                        return ServiceResult<RechargeResponseDTO>.Failure("Custom amount must be greater than zero");
                    }
                    
                    if (request.CustomAmount.Value > 10000) // Set reasonable maximum
                    {
                        return ServiceResult<RechargeResponseDTO>.Failure("Custom amount exceeds maximum limit of $10,000");
                    }
                    
                    rechargeAmount = request.CustomAmount.Value;
                }
                else
                {
                    return ServiceResult<RechargeResponseDTO>.Failure("Either package ID or custom amount must be provided");
                }

                // Validate payment method
                if (!Enum.IsDefined(typeof(PaymentMethod), request.PaymentMethod))
                {
                    return ServiceResult<RechargeResponseDTO>.Failure("Invalid payment method specified");
                }

                // Create transaction record
                var transaction = new Transaction
                {
                    UserId = request.UserId,
                    Amount = rechargeAmount,
                    TransactionType = TransactionType.Recharge,
                    Status = TransactionStatus.Pending,
                    RechargePackageId = packageId,
                    Description = packageId.HasValue ? "Package recharge" : "Custom recharge",
                    ReferenceNumber = GenerateReferenceNumber(),
                    TransactionDate = DateTime.UtcNow,
                    BalanceBefore = user.Balance ?? 0
                };

                await _unitOfWork.TransactionRepository.AddAsync(transaction);

                // Create payment intent with payment gateway
                var paymentResult = await _paymentService.CreatePaymentIntentAsync(
                    rechargeAmount,
                    "USD",
                    request.UserId,
                    (PaymentMethod)request.PaymentMethod
                );

                if (!paymentResult.IsSuccess)
                {
                    return ServiceResult<RechargeResponseDTO>.Failure(paymentResult.Message);
                }

                // Create payment transaction record
                var paymentTransaction = new PaymentTransaction
                {
                    TransactionId = transaction.TransactionId,
                    PaymentGateway = request.PaymentGateway ?? "Stripe",
                    PaymentIntentId = paymentResult.Data,
                    PaymentMethod = (PaymentMethod)request.PaymentMethod,
                    Amount = rechargeAmount,
                    Currency = "USD",
                    Status = TransactionStatus.Pending
                };

                await _unitOfWork.PaymentTransactionRepository.AddAsync(paymentTransaction);

                var response = new RechargeResponseDTO
                {
                    TransactionId = transaction.TransactionId,
                    Amount = rechargeAmount,
                    Status = TransactionStatus.Pending.ToString(),
                    PaymentIntentId = paymentResult.Data,
                    ReferenceNumber = transaction.ReferenceNumber,
                    CreatedAt = transaction.CreatedAt
                };

                _logger.LogInformation("Recharge initiated successfully for user {UserId}, transaction {TransactionId}",
                    request.UserId, transaction.TransactionId);

                return ServiceResult<RechargeResponseDTO>.Success(response, "Recharge initiated successfully");
            }, _unitOfWork, _logger, "Error initiating recharge");
        }

        public async Task<ServiceResult<bool>> ProcessRechargeCallbackAsync(string paymentIntentId, string status, string? failureReason = null)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                var paymentTransaction = await _unitOfWork.PaymentTransactionRepository.GetByPaymentIntentIdAsync(paymentIntentId);
                if (paymentTransaction == null)
                {
                    _logger.LogWarning("Payment transaction not found for intent ID: {PaymentIntentId}", paymentIntentId);
                    return ServiceResult<bool>.Failure("Payment transaction not found");
                }

                var transactionStatus = status.ToLower() switch
                {
                    "succeeded" => TransactionStatus.Completed,
                    "failed" => TransactionStatus.Failed,
                    "canceled" => TransactionStatus.Cancelled,
                    _ => TransactionStatus.Pending
                };

                // Update payment transaction status
                await _unitOfWork.PaymentTransactionRepository.UpdatePaymentStatusAsync(
                    paymentTransaction.PaymentTransactionId,
                    transactionStatus,
                    failureReason
                );

                // Update main transaction status
                await _unitOfWork.TransactionRepository.UpdateTransactionStatusAsync(
                    paymentTransaction.TransactionId,
                    transactionStatus
                );

                // If successful, update user balance
                if (transactionStatus == TransactionStatus.Completed)
                {
                    var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(paymentTransaction.TransactionId);
                    if (transaction != null)
                    {
                        var user = await _userManager.FindByIdAsync(transaction.UserId);
                        if (user != null)
                        {
                            var previousBalance = user.Balance ?? 0;
                            var newBalance = previousBalance + transaction.Amount;
                            
                            user.Balance = newBalance;
                            await _userManager.UpdateAsync(user);

                            // Update transaction balance records
                            await _unitOfWork.TransactionRepository.UpdateUserBalanceAsync(
                                user.Id,
                                newBalance,
                                previousBalance,
                                transaction.TransactionId
                            );

                            _logger.LogInformation("Recharge completed for user {UserId}, amount {Amount}",
                                user.Id, transaction.Amount);
                        }
                    }
                }

                return ServiceResult<bool>.Success(true, "Recharge callback processed successfully");
            }, _unitOfWork, _logger, $"Error processing recharge callback for intent {paymentIntentId}");
        }

        public async Task<ServiceResult<TransactionResponseDTO>> ConfirmRechargeAsync(Guid transactionId)
        {
            return await ServiceResult<TransactionResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transaction = await _unitOfWork.TransactionRepository.GetTransactionWithDetailsAsync(transactionId);
                if (transaction == null)
                {
                    return ServiceResult<TransactionResponseDTO>.Failure("Transaction not found");
                }

                if (transaction.Status != TransactionStatus.Completed)
                {
                    return ServiceResult<TransactionResponseDTO>.Failure("Transaction is not completed");
                }

                var responseDto = new TransactionResponseDTO
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
                    RechargePackageName = transaction.RechargePackage?.Name,
                    CreatedAt = transaction.CreatedAt,
                    UpdatedAt = transaction.UpdatedAt
                };

                return ServiceResult<TransactionResponseDTO>.Success(responseDto, "Recharge confirmed successfully");
            }, _unitOfWork, _logger, $"Error confirming recharge {transactionId}");
        }

        public async Task<ServiceResult<bool>> CancelRechargeAsync(Guid transactionId, string reason)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var transaction = await _unitOfWork.TransactionRepository.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    return ServiceResult<bool>.Failure("Transaction not found");
                }

                if (transaction.Status != TransactionStatus.Pending)
                {
                    return ServiceResult<bool>.Failure("Only pending transactions can be cancelled");
                }

                transaction.Status = TransactionStatus.Cancelled;
                transaction.Description = $"{transaction.Description} - Cancelled: {reason}";
                transaction.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.TransactionRepository.UpdateAsync(transaction);

                _logger.LogInformation("Recharge cancelled for transaction {TransactionId}, reason: {Reason}",
                    transactionId, reason);

                return ServiceResult<bool>.Success(true, "Recharge cancelled successfully");
            }, _unitOfWork, _logger, $"Error cancelling recharge {transactionId}");
        }

        public async Task<ServiceResult<IEnumerable<RechargePackage>>> GetActiveRechargePackagesAsync()
        {
            return await ServiceResult<IEnumerable<RechargePackage>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var packages = await _unitOfWork.RechargePackageRepository.GetActivePackagesAsync();
                return ServiceResult<IEnumerable<RechargePackage>>.Success(packages, "Active recharge packages retrieved successfully");
            }, _unitOfWork, _logger, "Error retrieving active recharge packages");
        }

        public async Task<ServiceResult<IEnumerable<RechargePackage>>> GetPromotionalPackagesAsync()
        {
            return await ServiceResult<IEnumerable<RechargePackage>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var packages = await _unitOfWork.RechargePackageRepository.GetPromotionalPackagesAsync();
                return ServiceResult<IEnumerable<RechargePackage>>.Success(packages, "Promotional packages retrieved successfully");
            }, _unitOfWork, _logger, "Error retrieving promotional packages");
        }

        private string GenerateReferenceNumber()
        {
            return $"RCH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }
    }

    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentService(ILogger<PaymentService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ServiceResult<string>> CreatePaymentIntentAsync(decimal amount, string currency, string userId, PaymentMethod paymentMethod)
        {
            try
            {
                // This is a mock implementation - replace with actual payment gateway integration
                // For Stripe, you would use Stripe.NET SDK here
                
                var paymentIntentId = $"pi_{Guid.NewGuid().ToString("N")[..24]}";
                
                _logger.LogInformation("Created payment intent {PaymentIntentId} for user {UserId}, amount {Amount}",
                    paymentIntentId, userId, amount);

                return ServiceResult<string>.Success(paymentIntentId, "Payment intent created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent for user {UserId}", userId);
                return ServiceResult<string>.Failure("Failed to create payment intent");
            }
        }

        public async Task<ServiceResult<bool>> ProcessPaymentWebhookAsync(string paymentIntentId, string status, string gatewayResponse)
        {
            try
            {
                // This is where you would handle webhook events from payment gateways
                _logger.LogInformation("Processing webhook for payment intent {PaymentIntentId}, status: {Status}",
                    paymentIntentId, status);

                // The actual webhook processing would be handled by the RechargeService
                return ServiceResult<bool>.Success(true, "Webhook processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment webhook");
                return ServiceResult<bool>.Failure("Failed to process webhook");
            }
        }

        public async Task<ServiceResult<PaymentTransaction>> GetPaymentByIntentIdAsync(string paymentIntentId)
        {
            try
            {
                // This would typically query your payment transaction repository
                // For now, returning a mock response
                return ServiceResult<PaymentTransaction>.Failure("Payment transaction not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment by intent ID {PaymentIntentId}", paymentIntentId);
                return ServiceResult<PaymentTransaction>.Failure("Failed to retrieve payment transaction");
            }
        }
    }
}
