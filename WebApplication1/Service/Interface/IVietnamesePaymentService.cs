using WebApplication1.DTO;
using WebApplication1.DTO.Transaction;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IVietnamesePaymentService
    {
        // Momo Payment Gateway
        Task<ServiceResult<PaymentResponseDTO>> CreateMomoPaymentAsync(PaymentRequestDTO request);
        Task<ServiceResult<PaymentStatusDTO>> ProcessMomoCallbackAsync(MomoCallbackDTO callback);
        
        // VNPay Gateway
        Task<ServiceResult<PaymentResponseDTO>> CreateVNPayPaymentAsync(PaymentRequestDTO request);
        Task<ServiceResult<PaymentStatusDTO>> ProcessVNPayCallbackAsync(VNPayCallbackDTO callback);
        
        // ZaloPay Gateway
        Task<ServiceResult<PaymentResponseDTO>> CreateZaloPayPaymentAsync(PaymentRequestDTO request);
        Task<ServiceResult<PaymentStatusDTO>> ProcessZaloPayCallbackAsync(ZaloPayCallbackDTO callback);
        
        // Banking Gateway (VietQR)
        Task<ServiceResult<PaymentResponseDTO>> CreateBankingPaymentAsync(PaymentRequestDTO request);
        Task<ServiceResult<PaymentStatusDTO>> ProcessBankingCallbackAsync(BankingCallbackDTO callback);
        
        // Utility methods
        Task<ServiceResult<string>> GenerateQRCodeAsync(string bankCode, string accountNumber, decimal amount, string content);
        Task<ServiceResult<bool>> ValidateWebhookSignatureAsync(string payload, string signature, PaymentGateway gateway);
    }

    // DTOs for Vietnamese payment gateways
    public class PaymentRequestDTO
    {
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentGateway Gateway { get; set; }
        public string? BankCode { get; set; }
        public string ReturnUrl { get; set; } = null!;
        public string CancelUrl { get; set; } = null!;
        public string NotifyUrl { get; set; } = null!;
        public string OrderDescription { get; set; } = null!;
        public string? ExtraData { get; set; }
    }

    public class PaymentResponseDTO
    {
        public string PaymentUrl { get; set; } = null!;
        public string PaymentId { get; set; } = null!;
        public string? QRCodeData { get; set; }
        public string Status { get; set; } = null!;
        public DateTime ExpiryTime { get; set; }
        public string? DeepLink { get; set; } // For mobile app integration
    }

    public class PaymentStatusDTO
    {
        public string PaymentId { get; set; } = null!;
        public TransactionStatus Status { get; set; }
        public string? FailureReason { get; set; }
        public decimal? ActualAmount { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? BankTransactionId { get; set; }
    }

    // Callback DTOs for different gateways
    public class MomoCallbackDTO
    {
        public string PartnerCode { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public string RequestId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = null!;
        public string OrderType { get; set; } = null!;
        public string TransId { get; set; } = null!;
        public int ResultCode { get; set; }
        public string Message { get; set; } = null!;
        public string PayType { get; set; } = null!;
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; } = null!;
        public string Signature { get; set; } = null!;
    }

    public class VNPayCallbackDTO
    {
        public string vnp_TmnCode { get; set; } = null!;
        public string vnp_Amount { get; set; } = null!;
        public string vnp_BankCode { get; set; } = null!;
        public string vnp_BankTranNo { get; set; } = null!;
        public string vnp_CardType { get; set; } = null!;
        public string vnp_OrderInfo { get; set; } = null!;
        public string vnp_PayDate { get; set; } = null!;
        public string vnp_ResponseCode { get; set; } = null!;
        public string vnp_TxnRef { get; set; } = null!;
        public string vnp_TransactionNo { get; set; } = null!;
        public string vnp_TransactionStatus { get; set; } = null!;
        public string vnp_SecureHash { get; set; } = null!;
    }

    public class ZaloPayCallbackDTO
    {
        public string AppId { get; set; } = null!;
        public string AppTransId { get; set; } = null!;
        public string AppTime { get; set; } = null!;
        public string AppUser { get; set; } = null!;
        public decimal Amount { get; set; }
        public string EmbedData { get; set; } = null!;
        public string Item { get; set; } = null!;
        public string ZpTransId { get; set; } = null!;
        public string ServerTime { get; set; } = null!;
        public string Channel { get; set; } = null!;
        public string MerchantUserId { get; set; } = null!;
        public string Mac { get; set; } = null!;
    }

    public class BankingCallbackDTO
    {
        public string BankCode { get; set; } = null!;
        public string TransactionId { get; set; } = null!;
        public string ReferenceId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Status { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime TransactionTime { get; set; }
        public string? Signature { get; set; }
    }
}
