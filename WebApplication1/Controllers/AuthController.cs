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

        public AuthController(UserManager<User> userManager ,ApplicationDbContext context, ITokenService tokenService, IAuthService authService)
        {
            _userManager = userManager;
            _context = context;
            _tokenService = tokenService;
            _authService = authService;
        }

        // Register User
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
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
            if (result != "User registered successfully.")
            {
                return BadRequest(result);
            }

            var jwtToken = await _tokenService.GenerateTokens(HttpContext, user);
            return Ok(jwtToken);  // Set AccessToken and RefreshToken in cookies
        }

        // Login action: generate both Access Token and Refresh Token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            string result = await _authService.LoginAsync(model);
            if(result != "Valid account.")
            {
                return BadRequest(result);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            var jwtToken = await _tokenService.GenerateTokens(HttpContext, user);
            return Ok(jwtToken);  // Set AccessToken and RefreshToken in cookies
        }

        // Verify Password (replace with actual hash verification logic)
        private async Task<bool> VerifyPasswordAsync(string enteredPassword, User user)
        {
            return await _userManager.CheckPasswordAsync(user, enteredPassword);
        }


        // Logout action: clear the Refresh Token cookie
        [HttpPost("logout")]
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
            var token = await _tokenService.RefreshToken(HttpContext);
            if (token == null)
            {
                return Unauthorized("Failed to refresh token. You must log out!");
            }
            return Ok(token);
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
