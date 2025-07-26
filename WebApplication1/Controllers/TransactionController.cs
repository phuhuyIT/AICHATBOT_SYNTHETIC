using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTO.Transaction;
using WebApplication1.Models;
using WebApplication1.Service.Interface;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IRechargeService _rechargeService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            ITransactionService transactionService,
            IRechargeService rechargeService,
            ILogger<TransactionController> logger)
        {
            _transactionService = transactionService;
            _rechargeService = rechargeService;
            _logger = logger;
        }

        #region CRUD Operations

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllTransactions()
        {
            try
            {
                var result = await _transactionService.GetAllAsync();
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllTransactions");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransactionById(Guid id)
        {
            try
            {
                var result = await _transactionService.GetByIdAsync(id);
                
                if (result.IsSuccess)
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                
                return NotFound(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTransactionById for ID {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionCreateDTO transactionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                var result = await _transactionService.CreateAsync(transactionDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    return CreatedAtAction(nameof(GetTransactionById), new { id = result.Data.TransactionId }, 
                        new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateTransaction");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion

        #region User Transaction History

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTransactions(string userId)
        {
            try
            {
                // Ensure users can only access their own transactions unless they're admin
                if (!User.IsInRole("Admin") && !IsCurrentUser(userId))
                {
                    return Forbid();
                }

                var result = await _transactionService.GetTransactionsByUserIdAsync(userId);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserTransactions for UserId {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("history")]
        public async Task<IActionResult> GetTransactionHistory([FromBody] TransactionHistoryFilterDTO filter)
        {
            try
            {
                // Ensure users can only access their own transaction history unless they're admin
                if (!User.IsInRole("Admin") && !IsCurrentUser(filter.UserId))
                {
                    return Forbid();
                }

                var result = await _transactionService.GetTransactionHistoryAsync(filter);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTransactionHistory");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion

        #region Admin Operations

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTransactionStatus(Guid id, [FromBody] int status)
        {
            try
            {
                if (!Enum.IsDefined(typeof(TransactionStatus), status))
                {
                    return BadRequest(new { success = false, message = "Invalid transaction status" });
                }

                var result = await _transactionService.UpdateTransactionStatusAsync(id, (TransactionStatus)status);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateTransactionStatus for ID {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpDelete("{id}/soft")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDeleteTransaction(Guid id)
        {
            try
            {
                var result = await _transactionService.SoftDeleteAsync(id);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return NotFound(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SoftDeleteTransaction for ID {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPatch("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestoreTransaction(Guid id)
        {
            try
            {
                var result = await _transactionService.RestoreAsync(id);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return NotFound(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RestoreTransaction for ID {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("deleted")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDeletedTransactions()
        {
            try
            {
                var result = await _transactionService.GetDeletedAsync();
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDeletedTransactions");
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
}
