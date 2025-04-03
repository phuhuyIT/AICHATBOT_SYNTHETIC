using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;
using WebApplication1.Service.Interface;
using WebApplication1.DTO.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Repository.Interface;
using Azure;


namespace WebApplication1.Service
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly IGenericRepository<RefreshToken> _tokenRepository;

        public TokenService(IConfiguration configuration, UserManager<User> userManager, IGenericRepository<RefreshToken> tokenRepository)
        {
            _configuration = configuration;
            _userManager = userManager;
            _tokenRepository = tokenRepository;
        }
        public async Task<JwtToken> GenerateTokens(HttpContext context, User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Save the refresh token to the database
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(Double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"])), // Set expiration date
                Created = DateTime.UtcNow,
            };

            await _tokenRepository.AddAsync(refreshTokenEntity);

            // Set the refresh token in an HTTP-only cookie
            context.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(Double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]))
            });

            return new JwtToken { AccessToken= accessToken};
        }
        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(Double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"])),  // Access token expiration
                signingCredentials: creds,
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"]
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }

        public async Task<bool> DeleteRefreshToken(HttpContext context)
        {
            var token = context.Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            //find refresh token
            var refreshToken = _tokenRepository.GetAllAsync().Result.FirstOrDefault(x => x.Token == token);
            if (refreshToken == null)
            {
                return false;
            }
            // delete refresh token
            await _tokenRepository.DeleteAsync(refreshToken.RefreshTokenID);
            return true;
        }

        public async Task<string> RefreshToken(HttpContext context)
        {
            var refreshToken = context.Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return null;
            }
            var refreshTokenEntity =  _tokenRepository.GetAllAsync().Result.FirstOrDefault(rt => rt.Token == refreshToken && rt.IsActive);
            if (refreshTokenEntity == null) {
                return null;
            }
            var user = _userManager.FindByIdAsync(refreshTokenEntity.UserId);
            if (user == null)
            {
                return null;
            }
            var accessToken = GenerateAccessToken(user.Result);
            return accessToken;
        }

        public bool RevokeToken(HttpContext context, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return false;
            }
            var refreshToken = context.Request.Cookies["RefreshToken"];
            var refreshTokenEntity = _tokenRepository.GetAllAsync().Result.FirstOrDefault(rt => rt.Token == refreshToken && rt.UserId == userId);
            if (refreshTokenEntity == null)
            {
                return false;
            }

            refreshTokenEntity.Revoked = DateTime.UtcNow;
            _tokenRepository.UpdateAsync(refreshTokenEntity);

            // Optionally delete cookies
            context.Response.Cookies.Delete("RefreshToken");
            return true;
        }
    }
}
