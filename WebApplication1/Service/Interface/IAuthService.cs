using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IAuthService
    {
        Task<ServiceResult<IdentityResult>> RegisterAsync(RegisterDTO registerDTO, User user);
        Task<ServiceResult<bool>> LoginAsync(LoginRequest loginDTO);
        Task<ServiceResult<bool>> ConfirmEmailAsync(string email, string token);
        Task<ServiceResult<bool>> ResendEmailConfirmationAsync(string email);
    }
}
