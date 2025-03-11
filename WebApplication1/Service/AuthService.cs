using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        public AuthService(UserManager<User> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        public async Task<string> LoginAsync(LoginRequest loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user == null)
            {
                return "Username is not exists.";
            }
            var result = await _userManager.CheckPasswordAsync(user, loginDTO.Password);
            if (!result)
            {
                return "Invalid password.";
            }
            return "Valid account.";
        }

        public async Task<string> RegisterAsync(RegisterDTO registerDTO, User user)
        {
            // check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(registerDTO.Email);
            if (existingUser != null)
            {
                return "Email already exists.";
            }
            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            if (!result.Succeeded)
            {
                return "An error occurred when create user.";
            }
            return "User registered successfully.";
        }
    }
}
