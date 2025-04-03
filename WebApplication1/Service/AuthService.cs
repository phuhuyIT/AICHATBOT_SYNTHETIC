using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        private readonly Interface.IEmailSender _emailSenderService;
        private readonly ApplicationDbContext db;
        public AuthService(UserManager<User> userManager, ITokenService tokenService, Interface.IEmailSender emailSender, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _emailSenderService = emailSender;
            db = dbContext;
        }

        public async Task<bool> LoginAsync(LoginRequest loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user == null)
            {
                throw new Exception("Không tìm thấy Email của bạn");
            }
            var result = await _userManager.CheckPasswordAsync(user, loginDTO.Password);
            if (!result)
            {
                throw new Exception("Sai mật khẩu.");
            }
            return true;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterDTO registerDTO, User user)
        {
            // Begin the EF Core transaction
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                // 1. Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(registerDTO.Email);
                if (existingUser != null)
                {
                    // Optionally rollback, then throw
                    await transaction.RollbackAsync();
                    throw new Exception("Email already exists.");
                }

                // 2. Create user
                var result = await _userManager.CreateAsync(user, registerDTO.Password);
                if (!result.Succeeded)
                {
                    // Rollback and return
                    await transaction.RollbackAsync();
                    return result;
                }
                await transaction.CommitAsync();
                // 3. Generate the email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var confirmationLink = $"https://localhost:7214/api/auth/confirmemail"
                                       + $"?email={user.Email}&token={System.Net.WebUtility.UrlEncode(token)}";
                var emailSubject = "Confirm your email";
                var emailTokens = new Dictionary<string, string>
                                    {
                                        { "USER_NAME", user.UserName },
                                        { "VERIFY_LINK", confirmationLink },
                                        { "YEAR", DateTime.Now.Year.ToString() }
                                    };
                var emailBody = await _emailSenderService.RenderTemplateAsync("RegisterEmailTemplate.html", emailTokens);
                await _emailSenderService.SendEmailAsync(user.Email, emailSubject, emailBody);

                return result;
            }
            catch
            {
                // If anything goes wrong, ensure the transaction is rolled back
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> ConfirmEmailAsync(string email, string token)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    throw new Exception("Email này không tồn tại, vui lòng kiểm tra lại!");
                } 

                var confirmResult = await _userManager.ConfirmEmailAsync(user, token);
                return confirmResult.Succeeded;
            }
            catch (Exception ex)
            {
                throw new Exception("Error confirming email", ex);
            }
        }
    }
}
