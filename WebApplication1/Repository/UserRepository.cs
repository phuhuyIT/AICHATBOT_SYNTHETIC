using Microsoft.AspNetCore.Identity;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserRepository(UserManager<User> userManager,RoleManager<IdentityRole> roleManager, ApplicationDbContext context) : base(context)
        {
            _userManager = userManager;
            _context = context;
            _roleManager = roleManager;
        }
        /// <summary>
        /// Add user to role. If role is not exist, create it.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<IdentityResult> AddUserToRoleAsync(User user, string roleName)
        {
            // Add role if role does not exist
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                try
                {
                    var role = new IdentityRole(roleName);
                    await _roleManager.CreateAsync(role);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error creating role: {ex.Message}");
                }
            }
            try
            {
                // Add user to role
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    return result;
                }
                else
                {
                    throw new Exception($"Error adding user to role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding user to role: {ex.Message}");
            }
        }
        /// <summary>
        /// Get user roles
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task<IList<string>> GetUserRolesAsync(User user)
        {
            // Get user roles
            var roles = _userManager.GetRolesAsync(user);
            if (roles == null)
            {
                throw new Exception("User has no roles");
            }
            return roles;
        }
        /// <summary>
        /// Check if user is in role
        /// </summary>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task<bool> IsUserInRoleAsync(User user, string role)
        {
            // Check is user in role
            var isInRole = _userManager.IsInRoleAsync(user, role);
            if (isInRole == null)
            {
                throw new Exception("User is not in role");
            }
            return isInRole;
        }
        /// <summary>
        /// Remove user from role
        /// </summary>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task<IdentityResult> RemoveUserFromRoleAsync(User user, string role)
        {
            // Remove user from role
            var result = _userManager.RemoveFromRoleAsync(user, role);
            if (result == null)
            {
                throw new Exception("Error removing user from role");
            }
            return result;
        }
    }
}
