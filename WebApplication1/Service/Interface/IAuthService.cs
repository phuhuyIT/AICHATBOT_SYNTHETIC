using Microsoft.AspNetCore.Identity.Data;
using WebApplication1.DTO.Auth;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDTO registerDTO, User user);
        Task<string> LoginAsync(LoginRequest loginDTO);
    }
}
