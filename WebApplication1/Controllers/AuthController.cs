using Azure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;
using WebApplication1.Service.Interface;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<User> userManager ,ApplicationDbContext context, ITokenService tokenService, IAuthService authService, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _context = context;
            _tokenService = tokenService;
            _authService = authService;
            _logger = logger;
        }
        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        // Register User
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            try
            {
                // Create the new user
                var user = new User
                {
                    UserName = model.Username,
                    Email = model.Email,
                    IsPaidUser = false,
                    Balance = 0
                };
                var result = await _authService.RegisterAsync(model, user);
                //var jwtToken = await _tokenService.GenerateTokens(HttpContext, user);
                return Ok("Please check your email");  // Set AccessToken and RefreshToken in cookies
            }
            catch (Exception message)
            {
                return BadRequest(message.Message);
            }
        }
        // Confirm Email
        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            _logger.LogInformation("ConfirmEmail request received for email: {Email}", email);
            
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("ConfirmEmail called with invalid parameters. Email: {Email}, Token provided: {TokenProvided}", 
                    email, !string.IsNullOrWhiteSpace(token));
                return BadRequest(new { 
                    message = "Email hoặc token xác nhận không hợp lệ.",
                    canResend = !string.IsNullOrWhiteSpace(email)
                });
            }

            try
            {
                var result = await _authService.ConfirmEmailAsync(email, token);
                if (result)
                {
                    _logger.LogInformation("Email confirmation successful for: {Email}", email);
                    return Ok(new { message = "Đăng ký thành công!", success = true });
                }
                
                _logger.LogWarning("Email confirmation failed for: {Email} - Unknown error", email);
                return BadRequest(new { 
                    message = "Xác nhận email thất bại!",
                    canResend = true,
                    email = email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email confirmation for: {Email}", email);
                
                // Check if it's a token-related error to suggest resending
                bool canResend = ex.Message.Contains("Token") || ex.Message.Contains("hết hạn") || ex.Message.Contains("không hợp lệ");
                
                return BadRequest(new { 
                    message = $"Xác nhận email thất bại: {ex.Message}",
                    canResend = canResend,
                    email = email
                });
            }
        }

        // Resend Email Confirmation
        [HttpPost("resend-email-confirmation")]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationDTO model)
        {
            _logger.LogInformation("ResendEmailConfirmation request received for email: {Email}", model.Email);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ResendEmailConfirmation called with invalid model state for email: {Email}", model.Email);
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _authService.ResendEmailConfirmationAsync(model.Email);
                if (result)
                {
                    _logger.LogInformation("Email confirmation resent successfully for: {Email}", model.Email);
                    return Ok(new { 
                        message = "Email xác nhận đã được gửi lại! Vui lòng kiểm tra hộp thư của bạn.",
                        success = true
                    });
                }
                
                _logger.LogWarning("Failed to resend email confirmation for: {Email}", model.Email);
                return BadRequest(new { message = "Không thể gửi lại email xác nhận!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resend email confirmation for: {Email}", model.Email);
                return BadRequest(new { message = $"Lỗi khi gửi lại email xác nhận: {ex.Message}" });
            }
        }

        // Login action: generate both Access Token and Refresh Token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            JwtToken jwtToken;
            try
            {
                bool result = await _authService.LoginAsync(model);
                var user = await _userManager.FindByEmailAsync(model.Email);
                jwtToken = await _tokenService.GenerateTokens(HttpContext, user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(jwtToken);  // Set AccessToken and RefreshToken in cookies
        }


        // Logout action: clear the Refresh Token cookie
        [HttpPost("logout")]
        [Authorize(Roles ="User")]
        public async Task<IActionResult> Logout()
        {
            // delete refresh token from database
            bool isDeleted = await _tokenService.DeleteRefreshToken(HttpContext);
            if (!isDeleted)
            {
                return BadRequest("Failed to revoke refresh token.");
            }
            // clear cookies
            Response.Cookies.Delete("refreshToken", new CookieOptions { HttpOnly = true, Secure = true }); // Clear the refresh token cookie

            return Ok("Logged out successfully");
        }

        // Refresh Access Token using Refresh Token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var token = await _tokenService.RefreshToken(HttpContext);
                if (token == null)
                {
                    return Unauthorized("Failed to refresh token. You must log out!");
                }
                return Ok(token);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost("revoke")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RevokeRefreshToken()
        {
            if(! _tokenService.RevokeToken(HttpContext, User))
            {
                return BadRequest("Failed to revoke refresh token.");
            }
            return Ok("Refresh token revoked successfully.");
        }

    }
}
