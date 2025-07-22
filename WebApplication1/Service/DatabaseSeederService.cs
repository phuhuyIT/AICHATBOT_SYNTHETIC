using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WebApplication1.Models;
using WebApplication1.DTO.Configuration;

namespace WebApplication1.Service;

public interface IDatabaseSeederService
{
    Task SeedSystemUserAsync();
    Task SeedAdminUserAsync();
    Task SeedAllAsync();
    Task<string> GetSystemUserIdAsync();
}

public class DatabaseSeederService : IDatabaseSeederService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly DefaultUsersSettings _defaultUsersSettings;

    public DatabaseSeederService(UserManager<User> userManager, RoleManager<Role> roleManager, IOptions<DefaultUsersSettings> defaultUsersSettings)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _defaultUsersSettings = defaultUsersSettings.Value;
    }

    public async Task SeedSystemUserAsync()
    {
        var systemUserConfig = _defaultUsersSettings.SystemUser;
        
        // Create System role if it doesn't exist
        if (!await _roleManager.RoleExistsAsync(systemUserConfig.RoleName))
        {
            var systemRole = new Role
            {
                Name = systemUserConfig.RoleName,
                Description = systemUserConfig.RoleDescription,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null, // This will be set after system user is created
                NormalizedName = systemUserConfig.RoleName.ToUpper()
            };
            await _roleManager.CreateAsync(systemRole);
        }

        // Create System user if it doesn't exist
        var systemUser = await _userManager.FindByEmailAsync(systemUserConfig.Email);
        if (systemUser == null)
        {
            systemUser = new User
            {
                UserName = systemUserConfig.Username,
                Email = systemUserConfig.Email,
                EmailConfirmed = true,
                IsActive = true,
                IsPaidUser = false,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(systemUser, systemUserConfig.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(systemUser, systemUserConfig.RoleName);
                
                // Update the system role's CreatedBy to reference the system user
                var role = await _roleManager.FindByNameAsync(systemUserConfig.RoleName);
                if (role != null)
                {
                    role.CreatedBy = systemUser.Id;
                    await _roleManager.UpdateAsync(role);
                }
            }
            else
            {
                throw new InvalidOperationException($"Failed to create system user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    public async Task SeedAdminUserAsync()
    {
        var adminUserConfig = _defaultUsersSettings.AdminUser;
        
        // Create Admin role if it doesn't exist
        if (!await _roleManager.RoleExistsAsync(adminUserConfig.RoleName))
        {
            var systemUserId = await GetSystemUserIdAsync();
            var adminRole = new Role
            {
                Name = adminUserConfig.RoleName,
                Description = adminUserConfig.RoleDescription,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = systemUserId,
                NormalizedName = adminUserConfig.RoleName.ToUpper()
            };
            await _roleManager.CreateAsync(adminRole);
        }

        // Create Admin user if it doesn't exist
        var adminUser = await _userManager.FindByNameAsync(adminUserConfig.Username);
        if (adminUser == null)
        {
            var systemUserId = await GetSystemUserIdAsync();
            adminUser = new User
            {
                UserName = adminUserConfig.Username,
                Email = adminUserConfig.Email,
                EmailConfirmed = true,
                IsActive = true,
                IsPaidUser = true, // Admin is a paid user by default
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(adminUser, adminUserConfig.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, adminUserConfig.RoleName);
            }
            else
            {
                throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    public async Task SeedAllAsync()
    {
        await SeedSystemUserAsync();
        await SeedAdminUserAsync();
    }

    public async Task<string> GetSystemUserIdAsync()
    {
        var systemUserConfig = _defaultUsersSettings.SystemUser;
        var systemUser = await _userManager.FindByEmailAsync(systemUserConfig.Email);
        if (systemUser == null)
        {
            await SeedSystemUserAsync();
            systemUser = await _userManager.FindByEmailAsync(systemUserConfig.Email);
        }
        
        return systemUser?.Id ?? throw new InvalidOperationException("System user not found and could not be created");
    }
}
