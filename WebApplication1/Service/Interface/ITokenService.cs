using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface ITokenService 
    {
        public Task<ServiceResult<JwtToken>> GenerateTokens(HttpContext context, User user);
        Task<ServiceResult<string>> GenerateAccessTokenAsync(User user);
        string GenerateRefreshToken();
        Task<ServiceResult<bool>> DeleteRefreshToken(HttpContext context);
        Task<ServiceResult<string>> RefreshToken(HttpContext context);
        ServiceResult<bool> RevokeToken(HttpContext context, ClaimsPrincipal user);
    }
}
