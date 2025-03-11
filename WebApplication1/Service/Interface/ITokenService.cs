using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface ITokenService 
    {
        public Task<JwtToken> GenerateTokens(HttpContext context, User user);
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        Task<bool> DeleteRefreshToken(HttpContext context);
        Task<string> RefreshToken(HttpContext context);
        bool RevokeToken(HttpContext context, ClaimsPrincipal user);
    }
}
