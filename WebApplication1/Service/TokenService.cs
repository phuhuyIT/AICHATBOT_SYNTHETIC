using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;
using WebApplication1.Service.Interface;
using WebApplication1.DTO.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Service
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, UserManager<User> userManager, IUnitOfWork unitOfWork, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResult<JwtToken>> GenerateTokens(HttpContext context, User user)
        {
            return await ServiceResult<JwtToken>.ExecuteWithTransactionAsync(async () =>
            {
                var accessToken = await GenerateAccessTokenAsync(user);
                var refreshToken = GenerateRefreshToken();

                // Save the refresh token to the database
                var refreshTokenEntity = CreateRefreshTokenEntity(user.Id, refreshToken);
                await AddRefreshTokenAsync(refreshTokenEntity);
                
                // Set the refresh token in an HTTP-only cookie
                SetRefreshTokenCookie(context, refreshToken);

                return ServiceResult<JwtToken>.Success(new JwtToken { AccessToken = accessToken.Data, RefreshToken = refreshToken });
            }, _unitOfWork, _logger, $"Error generating tokens for user {user.Id}");
        }

        public async Task<ServiceResult<string>> GenerateAccessTokenAsync(User user)
        {
            return await ServiceResult<string>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var getRoles = await _userManager.GetRolesAsync(user);
                var claims = CreateUserClaims(user, getRoles);

                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured")));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60")),
                    signingCredentials: creds,
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"]
                );

                return ServiceResult<string>.Success(new JwtSecurityTokenHandler().WriteToken(token));
            }, _unitOfWork, _logger, $"Error generating access token for user {user.Id}");
        }

        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }

        public async Task<ServiceResult<string>> RefreshToken(HttpContext context)
        {
            return await ServiceResult<string>.ExecuteWithTransactionAsync(async () =>
            {
                var refreshToken = context.Request.Cookies["RefreshToken"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return ServiceResult<string>.Failure("Refresh token not found");
                }

                // For now, we'll handle refresh tokens through a direct repository approach
                // Since RefreshToken might not be properly integrated into UnitOfWork yet
                var tokenEntity = await FindRefreshTokenAsync(refreshToken);
                
                if (tokenEntity == null || tokenEntity.ExpiryDate <= DateTime.UtcNow)
                {
                    return ServiceResult<string>.Failure("Invalid or expired refresh token");
                }

                // Get user and generate new access token
                var user = await _userManager.FindByIdAsync(tokenEntity.UserId);
                if (user == null)
                {
                    return ServiceResult<string>.Failure("User not found");
                }

                // Generate new access token
                var newAccessToken = await GenerateAccessTokenAsync(user);
                if (!newAccessToken.IsSuccess)
                {
                    return ServiceResult<string>.Failure($"Failed to generate access token: {newAccessToken.Message}");
                }

                // Generate new refresh token and update the database
                var newRefreshToken = GenerateRefreshToken();
                tokenEntity.Token = newRefreshToken;
                tokenEntity.ExpiryDate = DateTime.UtcNow.AddDays(Double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7"));
                
                await UpdateRefreshTokenAsync(tokenEntity);

                // Update cookie with new refresh token
                SetRefreshTokenCookie(context, newRefreshToken);

                return ServiceResult<string>.Success(newAccessToken.Data);
            }, _unitOfWork, _logger, "Error refreshing token");
        }

        public async Task<ServiceResult<bool>> RevokeToken(HttpContext context, ClaimsPrincipal user)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var refreshToken = context.Request.Cookies["RefreshToken"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return ServiceResult<bool>.Failure("Refresh token not found");
                }

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<bool>.Failure("User ID not found");
                }

                // Find and delete refresh token
                var tokenEntity = await FindRefreshTokenAsync(refreshToken);
                
                if (tokenEntity != null && tokenEntity.UserId == userId)
                {
                    await _unitOfWork.RefreshTokenRepository.DeleteAsync(tokenEntity.RefreshTokenID);;
                }

                // Remove the cookie
                context.Response.Cookies.Delete("RefreshToken");

                _logger.LogInformation("Token revoked for user {UserId}", userId);
                return ServiceResult<bool>.Success(true);
            }, _unitOfWork, _logger, "Error revoking token");
        }

        public async Task<ServiceResult<bool>> DeleteRefreshTokenAsync(HttpContext context)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var token = context.Request.Cookies["RefreshToken"];
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<bool>.Failure("Refresh token not found");
                }

                var refreshToken = await FindRefreshTokenAsync(token);
                if (refreshToken == null)
                {
                    return ServiceResult<bool>.Failure("Refresh token not found");
                }

                // Delete the refresh token
                await _unitOfWork.RefreshTokenRepository.DeleteAsync(refreshToken.RefreshTokenID);

                // Remove the cookie
                context.Response.Cookies.Delete("RefreshToken");

                return ServiceResult<bool>.Success(true);
            }, _unitOfWork, _logger, "Error deleting refresh token");
        }

        public async Task<ServiceResult<bool>> ValidateRefreshToken(string token)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                if (string.IsNullOrEmpty(token))
                    return ServiceResult<bool>.Failure("Refresh token not found");

                var refreshToken = await FindRefreshTokenAsync(token);
                var isValid = refreshToken != null && refreshToken.ExpiryDate > DateTime.UtcNow;
                if (!isValid)
                    return ServiceResult<bool>.Failure("Invalid or expired refresh token");

                return ServiceResult<bool>.Success(true);
            }, _unitOfWork, _logger, "Error validating refresh token");
        }

        #region Private Helper Methods

        private RefreshToken CreateRefreshTokenEntity(string userId, string refreshToken)
        {
            var expirationDays = Double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
            return new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(expirationDays),
            };
        }

        private static List<Claim> CreateUserClaims(User user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id),
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return claims;
        }

        private void SetRefreshTokenCookie(HttpContext context, string refreshToken)
        {
            var expirationDays = Double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
            context.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(expirationDays)
            });
        }

        // RefreshToken repository operations using UnitOfWork
        private async Task<RefreshToken?> FindRefreshTokenAsync(string token)
        {
            var allTokens = await _unitOfWork.RefreshTokenRepository.GetAllAsync();
            return allTokens.FirstOrDefault(x => x.Token == token);
        }

        private async Task UpdateRefreshTokenAsync(RefreshToken tokenEntity)
        {
            await _unitOfWork.RefreshTokenRepository.UpdateAsync(tokenEntity);
        }
        

        private async Task AddRefreshTokenAsync(RefreshToken tokenEntity)
        {
            await _unitOfWork.RefreshTokenRepository.AddAsync(tokenEntity);
        }
        #endregion
    }
}
