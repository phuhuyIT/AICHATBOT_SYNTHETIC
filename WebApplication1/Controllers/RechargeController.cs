using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTO.Transaction;
using WebApplication1.Service.Interface;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RechargeController : ControllerBase
    {
        private readonly IRechargeService _rechargeService;
        private readonly ILogger<RechargeController> _logger;

        public RechargeController(IRechargeService rechargeService, ILogger<RechargeController> logger)
        {
            _rechargeService = rechargeService;
            _logger = logger;
        }

        #region Recharge Operations

        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateRecharge([FromBody] RechargeRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                // Ensure users can only recharge their own account unless they're admin
                if (!User.IsInRole("Admin") && !IsCurrentUser(request.UserId))
                {
                    return Forbid();
                }

                var result = await _rechargeService.InitiateRechargeAsync(request);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InitiateRecharge");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("confirm/{transactionId}")]
        public async Task<IActionResult> ConfirmRecharge(Guid transactionId)
        {
            try
            {
                var result = await _rechargeService.ConfirmRechargeAsync(transactionId);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmRecharge for transaction {TransactionId}", transactionId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("cancel/{transactionId}")]
        public async Task<IActionResult> CancelRecharge(Guid transactionId, [FromBody] string reason)
        {
            try
            {
                var result = await _rechargeService.CancelRechargeAsync(transactionId, reason);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelRecharge for transaction {TransactionId}", transactionId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion

        #region Recharge Packages

        [HttpGet("packages")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveRechargePackages()
        {
            try
            {
                var result = await _rechargeService.GetActiveRechargePackagesAsync();
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActiveRechargePackages");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("packages/promotional")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPromotionalPackages()
        {
            try
            {
                var result = await _rechargeService.GetPromotionalPackagesAsync();
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPromotionalPackages");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion

        #region Payment Webhooks

        [HttpPost("webhook/callback")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessPaymentCallback([FromBody] PaymentCallbackDTO callback)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid callback data" });
                }

                var result = await _rechargeService.ProcessRechargeCallbackAsync(
                    callback.PaymentIntentId,
                    callback.Status,
                    callback.FailureReason
                );
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessPaymentCallback");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion

        #region Helper Methods

        private bool IsCurrentUser(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return currentUserId == userId;
        }

        #endregion
    }

    // DTO for payment callback
    public class PaymentCallbackDTO
    {
        public string PaymentIntentId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? FailureReason { get; set; }
    }
}
