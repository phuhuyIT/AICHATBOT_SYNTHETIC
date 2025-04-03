using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterDTO registerDTO, User user);
        Task<bool> LoginAsync(LoginRequest loginDTO);
        Task<bool> ConfirmEmailAsync(string email, string token);
    }
}
