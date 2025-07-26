using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebApplication1.DTO;
using WebApplication1.DTO.Transaction;
using WebApplication1.Models;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class VietnamesePaymentService : IVietnamesePaymentService
    {
        private readonly ILogger<VietnamesePaymentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public VietnamesePaymentService(
            ILogger<VietnamesePaymentService> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        #region Momo Payment Gateway

        public async Task<ServiceResult<PaymentResponseDTO>> CreateMomoPaymentAsync(PaymentRequestDTO request)
        {
            try
            {
                var momoConfig = _configuration.GetSection("MomoPayment");
                var partnerCode = momoConfig["PartnerCode"];
                var accessKey = momoConfig["AccessKey"];
                var secretKey = momoConfig["SecretKey"];
                var endpoint = momoConfig["Endpoint"];

                var orderId = $"MOMO_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
                var requestId = orderId;
                var amount = (long)(request.Amount * 100); // Convert to cents
                var orderInfo = request.OrderDescription;
                var redirectUrl = request.ReturnUrl;
                var ipnUrl = request.NotifyUrl;
                var extraData = request.ExtraData ?? "";

                // Create signature
                var rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType=captureWallet";
                var signature = CreateMomoSignature(rawHash, secretKey);

                var momoRequest = new
                {
                    partnerCode,
                    partnerName = "YourCompany",
                    storeId = "MomoTestStore",
                    requestId,
                    amount,
                    orderId,
                    orderInfo,
                    redirectUrl,
                    ipnUrl,
                    lang = "vi",
                    extraData,
                    requestType = "captureWallet",
                    signature
                };

                var jsonContent = JsonSerializer.Serialize(momoRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var momoResponse = JsonSerializer.Deserialize<MomoPaymentResponse>(responseContent);
                    
                    if (momoResponse?.ResultCode == 0)
                    {
                        _logger.LogInformation("Momo payment created successfully: {OrderId}", orderId);
                        
                        return ServiceResult<PaymentResponseDTO>.Success(new PaymentResponseDTO
                        {
                            PaymentUrl = momoResponse.PayUrl,
                            PaymentId = orderId,
                            QRCodeData = momoResponse.QrCodeUrl,
                            Status = "Pending",
                            ExpiryTime = DateTime.UtcNow.AddMinutes(15),
                            DeepLink = momoResponse.Deeplink
                        }, "Momo payment created successfully");
                    }
                    else
                    {
                        return ServiceResult<PaymentResponseDTO>.Failure($"Momo payment failed: {momoResponse?.Message}");
                    }
                }

                return ServiceResult<PaymentResponseDTO>.Failure("Failed to create Momo payment");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Momo payment");
                return ServiceResult<PaymentResponseDTO>.Failure("An error occurred while creating Momo payment");
            }
        }

        public async Task<ServiceResult<PaymentStatusDTO>> ProcessMomoCallbackAsync(MomoCallbackDTO callback)
        {
            try
            {
                var momoConfig = _configuration.GetSection("MomoPayment");
                var accessKey = momoConfig["AccessKey"];
                var secretKey = momoConfig["SecretKey"];

                // Verify signature
                var rawHash = $"accessKey={accessKey}&amount={callback.Amount}&extraData={callback.ExtraData}&message={callback.Message}&orderId={callback.OrderId}&orderInfo={callback.OrderInfo}&orderType={callback.OrderType}&partnerCode={callback.PartnerCode}&payType={callback.PayType}&requestId={callback.RequestId}&responseTime={callback.ResponseTime}&resultCode={callback.ResultCode}&transId={callback.TransId}";
                var expectedSignature = CreateMomoSignature(rawHash, secretKey);

                if (expectedSignature != callback.Signature)
                {
                    _logger.LogWarning("Invalid Momo callback signature for order {OrderId}", callback.OrderId);
                    return ServiceResult<PaymentStatusDTO>.Failure("Invalid signature");
                }

                var status = callback.ResultCode == 0 ? TransactionStatus.Completed : TransactionStatus.Failed;
                var failureReason = callback.ResultCode != 0 ? callback.Message : null;

                _logger.LogInformation("Momo callback processed for order {OrderId}, status: {Status}", callback.OrderId, status);

                return ServiceResult<PaymentStatusDTO>.Success(new PaymentStatusDTO
                {
                    PaymentId = callback.OrderId,
                    Status = status,
                    FailureReason = failureReason,
                    ActualAmount = callback.Amount / 100, // Convert back from cents
                    CompletedAt = status == TransactionStatus.Completed ? DateTime.UtcNow : null,
                    BankTransactionId = callback.TransId
                }, "Momo callback processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Momo callback");
                return ServiceResult<PaymentStatusDTO>.Failure("An error occurred while processing Momo callback");
            }
        }

        #endregion

        #region VNPay Gateway

        public async Task<ServiceResult<PaymentResponseDTO>> CreateVNPayPaymentAsync(PaymentRequestDTO request)
        {
            try
            {
                var vnpayConfig = _configuration.GetSection("VNPayment");
                var tmnCode = vnpayConfig["TmnCode"];
                var hashSecret = vnpayConfig["HashSecret"];
                var baseUrl = vnpayConfig["BaseUrl"];

                var vnpayData = new SortedDictionary<string, string>
                {
                    ["vnp_Version"] = "2.1.0",
                    ["vnp_Command"] = "pay",
                    ["vnp_TmnCode"] = tmnCode,
                    ["vnp_Amount"] = ((long)(request.Amount * 100)).ToString(), // VNPay uses cents
                    ["vnp_CurrCode"] = "VND",
                    ["vnp_TxnRef"] = $"VNP_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}",
                    ["vnp_OrderInfo"] = request.OrderDescription,
                    ["vnp_OrderType"] = "other",
                    ["vnp_Locale"] = "vn",
                    ["vnp_ReturnUrl"] = request.ReturnUrl,
                    ["vnp_IpAddr"] = "127.0.0.1", // Should get real IP
                    ["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                };

                if (!string.IsNullOrEmpty(request.BankCode))
                {
                    vnpayData["vnp_BankCode"] = request.BankCode;
                }

                // Create signature
                var query = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
                var signature = CreateVNPaySignature(query, hashSecret);
                
                var paymentUrl = $"{baseUrl}?{query}&vnp_SecureHash={signature}";

                _logger.LogInformation("VNPay payment created for order {TxnRef}", vnpayData["vnp_TxnRef"]);

                return ServiceResult<PaymentResponseDTO>.Success(new PaymentResponseDTO
                {
                    PaymentUrl = paymentUrl,
                    PaymentId = vnpayData["vnp_TxnRef"],
                    Status = "Pending",
                    ExpiryTime = DateTime.UtcNow.AddMinutes(15)
                }, "VNPay payment created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment");
                return ServiceResult<PaymentResponseDTO>.Failure("An error occurred while creating VNPay payment");
            }
        }

        public async Task<ServiceResult<PaymentStatusDTO>> ProcessVNPayCallbackAsync(VNPayCallbackDTO callback)
        {
            try
            {
                var vnpayConfig = _configuration.GetSection("VNPayment");
                var hashSecret = vnpayConfig["HashSecret"];

                // Verify signature
                var vnpayData = new SortedDictionary<string, string>
                {
                    ["vnp_Amount"] = callback.vnp_Amount,
                    ["vnp_BankCode"] = callback.vnp_BankCode,
                    ["vnp_BankTranNo"] = callback.vnp_BankTranNo,
                    ["vnp_CardType"] = callback.vnp_CardType,
                    ["vnp_OrderInfo"] = callback.vnp_OrderInfo,
                    ["vnp_PayDate"] = callback.vnp_PayDate,
                    ["vnp_ResponseCode"] = callback.vnp_ResponseCode,
                    ["vnp_TmnCode"] = callback.vnp_TmnCode,
                    ["vnp_TransactionNo"] = callback.vnp_TransactionNo,
                    ["vnp_TransactionStatus"] = callback.vnp_TransactionStatus,
                    ["vnp_TxnRef"] = callback.vnp_TxnRef
                };

                var query = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
                var expectedSignature = CreateVNPaySignature(query, hashSecret);

                if (expectedSignature.ToLower() != callback.vnp_SecureHash.ToLower())
                {
                    _logger.LogWarning("Invalid VNPay callback signature for order {TxnRef}", callback.vnp_TxnRef);
                    return ServiceResult<PaymentStatusDTO>.Failure("Invalid signature");
                }

                var status = callback.vnp_ResponseCode == "00" ? TransactionStatus.Completed : TransactionStatus.Failed;
                var failureReason = callback.vnp_ResponseCode != "00" ? $"VNPay error code: {callback.vnp_ResponseCode}" : null;

                _logger.LogInformation("VNPay callback processed for order {TxnRef}, status: {Status}", callback.vnp_TxnRef, status);

                return ServiceResult<PaymentStatusDTO>.Success(new PaymentStatusDTO
                {
                    PaymentId = callback.vnp_TxnRef,
                    Status = status,
                    FailureReason = failureReason,
                    ActualAmount = decimal.Parse(callback.vnp_Amount) / 100,
                    CompletedAt = status == TransactionStatus.Completed ? DateTime.ParseExact(callback.vnp_PayDate, "yyyyMMddHHmmss", null) : null,
                    BankTransactionId = callback.vnp_TransactionNo
                }, "VNPay callback processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay callback");
                return ServiceResult<PaymentStatusDTO>.Failure("An error occurred while processing VNPay callback");
            }
        }

        #endregion

        #region ZaloPay Gateway

        public async Task<ServiceResult<PaymentResponseDTO>> CreateZaloPayPaymentAsync(PaymentRequestDTO request)
        {
            try
            {
                var zaloConfig = _configuration.GetSection("ZaloPayment");
                var appId = zaloConfig["AppId"];
                var key1 = zaloConfig["Key1"];
                var endpoint = zaloConfig["Endpoint"];

                var transId = $"ZLP_{DateTime.UtcNow:yyyyMMdd}_{new Random().Next(1000000)}";
                var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var embedData = JsonSerializer.Serialize(new { redirecturl = request.ReturnUrl });

                var orderData = new Dictionary<string, object>
                {
                    ["app_id"] = int.Parse(appId),
                    ["app_user"] = request.UserId,
                    ["app_time"] = appTime,
                    ["amount"] = (long)request.Amount,
                    ["app_trans_id"] = transId,
                    ["embed_data"] = embedData,
                    ["item"] = JsonSerializer.Serialize(new[] { new { itemid = "1", itemname = request.OrderDescription, itemprice = (long)request.Amount, itemquantity = 1 } }),
                    ["description"] = request.OrderDescription,
                    ["bank_code"] = "",
                    ["callback_url"] = request.NotifyUrl
                };

                // Create MAC
                var data = $"{orderData["app_id"]}|{orderData["app_trans_id"]}|{orderData["app_user"]}|{orderData["amount"]}|{orderData["app_time"]}|{orderData["embed_data"]}|{orderData["item"]}";
                var mac = CreateZaloPaySignature(data, key1);
                orderData["mac"] = mac;

                var jsonContent = JsonSerializer.Serialize(orderData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var zaloResponse = JsonSerializer.Deserialize<ZaloPaymentResponse>(responseContent);
                    
                    if (zaloResponse?.ReturnCode == 1)
                    {
                        _logger.LogInformation("ZaloPay payment created successfully: {TransId}", transId);
                        
                        return ServiceResult<PaymentResponseDTO>.Success(new PaymentResponseDTO
                        {
                            PaymentUrl = zaloResponse.OrderUrl,
                            PaymentId = transId,
                            Status = "Pending",
                            ExpiryTime = DateTime.UtcNow.AddMinutes(15)
                        }, "ZaloPay payment created successfully");
                    }
                    else
                    {
                        return ServiceResult<PaymentResponseDTO>.Failure($"ZaloPay payment failed: {zaloResponse?.ReturnMessage}");
                    }
                }

                return ServiceResult<PaymentResponseDTO>.Failure("Failed to create ZaloPay payment");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ZaloPay payment");
                return ServiceResult<PaymentResponseDTO>.Failure("An error occurred while creating ZaloPay payment");
            }
        }

        public async Task<ServiceResult<PaymentStatusDTO>> ProcessZaloPayCallbackAsync(ZaloPayCallbackDTO callback)
        {
            try
            {
                var zaloConfig = _configuration.GetSection("ZaloPayment");
                var key2 = zaloConfig["Key2"];

                // Verify MAC
                var data = $"{callback.AppId}|{callback.AppTransId}|{callback.AppUser}|{callback.Amount}|{callback.AppTime}|{callback.EmbedData}|{callback.Item}|{callback.ZpTransId}|{callback.ServerTime}|{callback.Channel}|{callback.MerchantUserId}";
                var expectedMac = CreateZaloPaySignature(data, key2);

                if (expectedMac != callback.Mac)
                {
                    _logger.LogWarning("Invalid ZaloPay callback signature for order {AppTransId}", callback.AppTransId);
                    return ServiceResult<PaymentStatusDTO>.Failure("Invalid signature");
                }

                _logger.LogInformation("ZaloPay callback processed for order {AppTransId}", callback.AppTransId);

                return ServiceResult<PaymentStatusDTO>.Success(new PaymentStatusDTO
                {
                    PaymentId = callback.AppTransId,
                    Status = TransactionStatus.Completed,
                    ActualAmount = callback.Amount,
                    CompletedAt = DateTime.UtcNow,
                    BankTransactionId = callback.ZpTransId
                }, "ZaloPay callback processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ZaloPay callback");
                return ServiceResult<PaymentStatusDTO>.Failure("An error occurred while processing ZaloPay callback");
            }
        }

        #endregion

        #region Banking Gateway

        public async Task<ServiceResult<PaymentResponseDTO>> CreateBankingPaymentAsync(PaymentRequestDTO request)
        {
            try
            {
                // For banking gateway, we generate QR code or provide bank transfer instructions
                var bankConfig = _configuration.GetSection("BankingPayment");
                var bankAccount = bankConfig["BankAccount"];
                var bankName = bankConfig["BankName"];
                var accountHolder = bankConfig["AccountHolder"];

                var transactionId = $"BANK_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
                var transferContent = $"Nap tien {transactionId}";

                // Generate VietQR code
                var qrResult = await GenerateQRCodeAsync(request.BankCode!, bankAccount, request.Amount, transferContent);
                
                if (!qrResult.IsSuccess)
                {
                    return ServiceResult<PaymentResponseDTO>.Failure("Failed to generate QR code");
                }

                _logger.LogInformation("Banking payment created for transaction {TransactionId}", transactionId);

                return ServiceResult<PaymentResponseDTO>.Success(new PaymentResponseDTO
                {
                    PaymentUrl = request.ReturnUrl, // Return to confirmation page
                    PaymentId = transactionId,
                    QRCodeData = qrResult.Data,
                    Status = "Pending",
                    ExpiryTime = DateTime.UtcNow.AddHours(24) // Banking transfers have longer expiry
                }, "Banking payment created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating banking payment");
                return ServiceResult<PaymentResponseDTO>.Failure("An error occurred while creating banking payment");
            }
        }

        public async Task<ServiceResult<PaymentStatusDTO>> ProcessBankingCallbackAsync(BankingCallbackDTO callback)
        {
            try
            {
                // Validate banking callback (usually from bank webhook or manual verification)
                var status = callback.Status.ToLower() == "success" ? TransactionStatus.Completed : TransactionStatus.Failed;

                _logger.LogInformation("Banking callback processed for transaction {TransactionId}, status: {Status}", 
                    callback.TransactionId, status);

                return ServiceResult<PaymentStatusDTO>.Success(new PaymentStatusDTO
                {
                    PaymentId = callback.ReferenceId,
                    Status = status,
                    ActualAmount = callback.Amount,
                    CompletedAt = status == TransactionStatus.Completed ? callback.TransactionTime : null,
                    BankTransactionId = callback.TransactionId
                }, "Banking callback processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing banking callback");
                return ServiceResult<PaymentStatusDTO>.Failure("An error occurred while processing banking callback");
            }
        }

        #endregion

        #region Utility Methods

        public async Task<ServiceResult<string>> GenerateQRCodeAsync(string bankCode, string accountNumber, decimal amount, string content)
        {
            try
            {
                // Generate VietQR URL
                var qrUrl = $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.png?amount={amount}&addInfo={Uri.EscapeDataString(content)}&accountName={Uri.EscapeDataString("Your Company Name")}";
                
                return ServiceResult<string>.Success(qrUrl, "QR code generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                return ServiceResult<string>.Failure("Failed to generate QR code");
            }
        }

        public async Task<ServiceResult<bool>> ValidateWebhookSignatureAsync(string payload, string signature, PaymentGateway gateway)
        {
            try
            {
                // Implement signature validation based on gateway
                switch (gateway)
                {
                    case PaymentGateway.MoMo:
                        // Momo signature validation logic
                        break;
                    case PaymentGateway.VNPay:
                        // VNPay signature validation logic
                        break;
                    case PaymentGateway.ZaloPay:
                        // ZaloPay signature validation logic
                        break;
                }

                return ServiceResult<bool>.Success(true, "Signature validated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook signature");
                return ServiceResult<bool>.Failure("Failed to validate signature");
            }
        }

        #endregion

        #region Private Helper Methods

        private string CreateMomoSignature(string data, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }

        private string CreateVNPaySignature(string data, string secretKey)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }

        private string CreateZaloPaySignature(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }

        #endregion
    }

    #region Response Models

    public class MomoPaymentResponse
    {
        public string PartnerCode { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public string RequestId { get; set; } = null!;
        public long Amount { get; set; }
        public long ResponseTime { get; set; }
        public string Message { get; set; } = null!;
        public int ResultCode { get; set; }
        public string PayUrl { get; set; } = null!;
        public string Deeplink { get; set; } = null!;
        public string QrCodeUrl { get; set; } = null!;
    }

    public class ZaloPaymentResponse
    {
        public int ReturnCode { get; set; }
        public string ReturnMessage { get; set; } = null!;
        public string OrderUrl { get; set; } = null!;
        public string ZpTransToken { get; set; } = null!;
    }

    #endregion
}
