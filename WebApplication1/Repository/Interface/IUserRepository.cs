using Microsoft.AspNetCore.Identity;
using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IUserRepository : IGenericRepository <User>
    {
        Task<IdentityResult> AddUserToRoleAsync(User user, string role);
        Task<IdentityResult> RemoveUserFromRoleAsync(User user, string role);
        Task<IList<string>> GetUserRolesAsync(User user);
        Task<bool> IsUserInRoleAsync(User user, string role);
    }
}
