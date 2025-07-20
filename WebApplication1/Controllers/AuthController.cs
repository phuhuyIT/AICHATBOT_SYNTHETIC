using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;
using WebApplication1.Service;
using WebApplication1.Service.Interface;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(
        UserManager<User> userManager,
        ITokenService tokenService,
        IAuthService authService,
        ILogger<AuthController> logger) : ControllerBase
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IAuthService _authService = authService;
        private readonly ILogger<AuthController> _logger = logger;

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
            }

            // Create the new user
            var user = new User
            {
                UserName = model.Username,
                Email = model.Email,
                IsPaidUser = false,
                Balance = 0
            };

            var result = await _authService.RegisterAsync(model, user);
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, data = result.Data, message = result.Message });
        }

        /// <summary>
        /// Confirm Email
        /// </summary>
        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            _logger.LogInformation("ConfirmEmail request received for email: {Email}", email);

            var result = await _authService.ConfirmEmailAsync(email, token);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Email confirmation failed for: {Email} - {Message}", email, result.Message);
                return BadRequest(new { success = false, message = result.Message });
            }
            
            _logger.LogInformation("Email confirmation successful for: {Email}", email);
            return Ok(new { success = true, message = "Email xác nhận thành công!" });
        }

        /// <summary>
        /// Resend Email Confirmation
        /// </summary>
        [HttpPost("resend-email-confirmation")]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationDTO model)
        {
            _logger.LogInformation("ResendEmailConfirmation request received for email: {Email}", model.Email);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ResendEmailConfirmation called with invalid model state for email: {Email}", model.Email);
                return BadRequest(new { success = false, message = "Invalid model state" });
            }

            var result = await _authService.ResendEmailConfirmationAsync(model.Email);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to resend email confirmation for: {Email} - {Message}", model.Email, result.Message);
                return BadRequest(new { success = false, message = result.Message });
            }
            
            _logger.LogInformation("Email confirmation resent successfully for: {Email}", model.Email);
            return Ok(new { success = true, message = "Gửi email xác nhận thành công!" });
        }

        /// <summary>
        /// Login action: generate both Access Token and Refresh Token
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid model state" });
            }

            var loginResult = await _authService.LoginAsync(model);
            if (!loginResult.IsSuccess)
            {
                return BadRequest(new { success = false, message = loginResult.Message });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest(new { success = false, message = "User not found" });
            }

            var tokenResult = await _tokenService.GenerateTokens(HttpContext, user);
            if (!tokenResult.IsSuccess || tokenResult.Data == null)
            {
                return BadRequest(new { success = false, message = "Failed to generate tokens" });
            }

            return Ok(new { success = true, data = tokenResult.Data, message = "Login successful" });
        }

        /// <summary>
        /// Logout action: clear the Refresh Token cookie
        /// </summary>
        [HttpPost("logout")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Logout()
        {
            var deleteResult = await _tokenService.DeleteRefreshToken(HttpContext);
            if (!deleteResult.IsSuccess || !deleteResult.Data)
            {
                return BadRequest(new { success = false, message = "Failed to delete refresh token" });
            }

            // clear cookies
            Response.Cookies.Delete("refreshToken", new CookieOptions { HttpOnly = true, Secure = true });

            return Ok(new { success = true, message = "Logout successful" });
        }

        /// <summary>
        /// Refresh Access Token using Refresh Token
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var tokenResult = await _tokenService.RefreshToken(HttpContext);
            if (!tokenResult.IsSuccess || tokenResult.Data == null)
            {
                return Unauthorized(new { success = false, message = "Failed to refresh token" });
            }

            return Ok(new { success = true, data = tokenResult.Data, message = "Token refreshed successfully" });
        }

        /// <summary>
        /// Revoke refresh token
        /// </summary>
        [HttpPost("revoke")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RevokeRefreshToken()
        {
            var revokeResult = _tokenService.RevokeToken(HttpContext, User);
            if (!revokeResult.IsSuccess || !revokeResult.Data)
            {
                return BadRequest(new { success = false, message = "Failed to revoke refresh token" });
            }

            return Ok(new { success = true, message = "Refresh token revoked successfully" });
        }
    }
}
