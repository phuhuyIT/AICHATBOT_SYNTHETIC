using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly Interface.IEmailSender _emailSenderService;
        private readonly ApplicationDbContext db;
        private readonly ILogger<AuthService> _logger;
        
        public AuthService(UserManager<User> userManager, ITokenService tokenService, Interface.IEmailSender emailSender, ApplicationDbContext dbContext, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailSenderService = emailSender;
            db = dbContext;
            _logger = logger;
        }

        public async Task<bool> LoginAsync(LoginRequest loginDTO)
        {
            var user = await FindUserByEmailAsync(loginDTO.Email);
            
            var result = await _userManager.CheckPasswordAsync(user, loginDTO.Password);
            if (!result)
            {
                throw new Exception("Sai mật khẩu.");
            }
            return true;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterDTO registerDTO, User user)
        {
            IdentityResult result;
    
            // Begin transaction for user creation only
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(registerDTO.Email);
                if (existingUser != null)
                {
                    throw new Exception("Email already exists.");
                }

                result = await _userManager.CreateAsync(user, registerDTO.Password);
                if (!result.Succeeded)
                {
                    // 1. Build a single readable message from Identity errors
                    var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));

                    // 2. Log the details (good for server-side diagnostics)
                    _logger.LogError("User creation failed for email {Email}. Errors: {Errors}",
                        registerDTO.Email, errorMessages);

                    // 3. Throw one descriptive exception so the controller can return 400
                    throw new Exception($"Có lỗi khi tạo tài khoản: {errorMessages}");
                }   
                
                await _userManager.AddToRoleAsync(user, "User");
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Send email after successful user creation
            try
            {
                await SendEmailConfirmationAsync(user, "Confirm your email");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw new Exception("Có lỗi khi gửi email sau khi tạo tài khoản.");
            }

            return result;
        }

        public async Task<bool> ConfirmEmailAsync(string email, string token)
        {
            _logger.LogInformation("Starting email confirmation process for: {Email}", email);
            
            ValidateEmailInput(email);
            ValidateTokenInput(token, email);
            
            var user = await FindUserByEmailAsync(email);
            _logger.LogInformation("User found for email confirmation: {Email}, UserId: {UserId}", email, user.Id);

            ValidateEmailNotAlreadyConfirmed(user);

            // Attempt to confirm email
            var confirmResult = await _userManager.ConfirmEmailAsync(user, token);
            if (!confirmResult.Succeeded)
            {
                var errors = string.Join(", ", confirmResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Email confirmation failed for user: {Email}, UserId: {UserId}. Errors: {Errors}", 
                    email, user.Id, errors);
                
                // Check for specific error types
                if (confirmResult.Errors.Any(e => e.Code.Contains("InvalidToken") || e.Description.Contains("Invalid token")))
                {
                    throw new Exception("Token xác nhận không hợp lệ hoặc đã hết hạn! Vui lòng yêu cầu gửi lại email xác nhận.");
                }
                
                throw new Exception($"Xác nhận email thất bại: {errors}. Bạn có thể thử gửi lại email xác nhận.");
            }

            _logger.LogInformation("Email confirmation successful for user: {Email}, UserId: {UserId}", email, user.Id);
            return true;
        }

        public async Task<bool> ResendEmailConfirmationAsync(string email)
        {
            _logger.LogInformation("Starting resend email confirmation process for: {Email}", email);
            
            ValidateEmailInput(email);
            
            var user = await FindUserByEmailAsync(email);
            _logger.LogInformation("User found for resend email confirmation: {Email}, UserId: {UserId}", email, user.Id);

            ValidateEmailNotAlreadyConfirmed(user);

            try
            {
                await SendEmailConfirmationAsync(user, "Confirm your email - Resent");
                _logger.LogInformation("Email confirmation resent successfully for user: {Email}, UserId: {UserId}", email, user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email confirmation for user: {Email}, UserId: {UserId}", email, user.Id);
                throw new Exception($"Có lỗi khi gửi email xác nhận: {ex.Message}");
            }
        }

        // Helper methods to eliminate duplicate code
        private void ValidateEmailInput(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Method called with null or empty email");
                throw new ArgumentException("Email không được để trống.");
            }
        }

        private void ValidateTokenInput(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Method called with null or empty token for email: {Email}", email);
                throw new ArgumentException("Token xác nhận không được để trống.");
            }
        }

        private async Task<User> FindUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("User not found for email: {Email}", email);
                throw new Exception("Email này không tồn tại trong hệ thống, vui lòng kiểm tra lại!");
            }
            return user;
        }

        private void ValidateEmailNotAlreadyConfirmed(User user)
        {
            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email already confirmed for user: {Email}, UserId: {UserId}", user.Email, user.Id);
                throw new Exception("Email đã được xác nhận trước đó! Bạn có thể đăng nhập ngay bây giờ.");
            }
        }

        private async Task SendEmailConfirmationAsync(User user, string subject)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = GenerateConfirmationLink(user.Email, token);
            
            var emailTokens = new Dictionary<string, string>
            {
                { "USER_NAME", user.UserName },
                { "VERIFY_LINK", confirmationLink },
                { "YEAR", DateTime.Now.Year.ToString() }
            };
            
            var emailBody = await _emailSenderService.RenderTemplateAsync("RegisterEmailTemplate.html", emailTokens);
            await _emailSenderService.SendEmailAsync(user.Email, subject, emailBody);
        }

        private string GenerateConfirmationLink(string email, string token)
        {
            return $"https://localhost:7214/api/auth/confirmemail?email={email}&token={System.Net.WebUtility.UrlEncode(token)}";
        }
    }
}
