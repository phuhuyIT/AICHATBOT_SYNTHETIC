using Microsoft.AspNetCore.Identity;
using WebApplication1.DTO;
using WebApplication1.DTO.Role;
using WebApplication1.Models;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service;

public class RoleService : IRoleService
{
    private readonly ILogger<RoleService> _logger;
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;

    public RoleService(ILogger<RoleService> logger, 
        RoleManager<Role> roleManager,
        UserManager<User> userManager)
    {
        _logger = logger;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    #region IReadService Implementation

    public async Task<ServiceResult<IEnumerable<RoleResponseDTO>>> GetAllAsync()
    {
        try
        {
            var roles = _roleManager.Roles.ToList();
            var roleDtos = roles.Select(role => new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedBy = role.UpdatedBy
            }).ToList();

            return ServiceResult<IEnumerable<RoleResponseDTO>>.Success(roleDtos, "Roles retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all roles");
            return ServiceResult<IEnumerable<RoleResponseDTO>>.Failure("Failed to retrieve roles");
        }
    }

    public async Task<ServiceResult<RoleResponseDTO>> GetByIdAsync(Guid id)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return ServiceResult<RoleResponseDTO>.Failure("Role not found");
            }

            var roleDto = new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedBy = role.UpdatedBy
            };

            return ServiceResult<RoleResponseDTO>.Success(roleDto, "Role retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with ID {RoleId}", id);
            return ServiceResult<RoleResponseDTO>.Failure("Failed to retrieve role");
        }
    }

    #endregion

    #region IWriteService Implementation

    public async Task<ServiceResult<RoleResponseDTO>> CreateAsync(RoleCreateDTO createDto)
    {
        if (createDto == null)
        {
            _logger.LogError("RoleCreateDTO is null");
            return ServiceResult<RoleResponseDTO>.Failure("Role data is null");
        }

        try
        {
            // Check if role with same name already exists
            var existingRole = await _roleManager.FindByNameAsync(createDto.Name);
            if (existingRole != null)
            {
                return ServiceResult<RoleResponseDTO>.Failure("Role with this name already exists");
            }

            var role = new Role
            {
                Name = createDto.Name,
                Description = createDto.Description,
                IsActive = createDto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create role: {Errors}", errors);
                return ServiceResult<RoleResponseDTO>.Failure($"Failed to create role: {errors}");
            }

            var roleDto = new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedBy = role.UpdatedBy
            };

            return ServiceResult<RoleResponseDTO>.Success(roleDto, "Role created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return ServiceResult<RoleResponseDTO>.Failure("Failed to create role");
        }
    }

    public async Task<ServiceResult<RoleResponseDTO>> UpdateAsync(Guid id, RoleUpdateDTO updateDto)
    {
        if (updateDto == null)
        {
            _logger.LogError("RoleUpdateDTO is null");
            return ServiceResult<RoleResponseDTO>.Failure("Role data is null");
        }

        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return ServiceResult<RoleResponseDTO>.Failure("Role not found");
            }

            // Check if another role with the same name exists
            var existingRole = await _roleManager.FindByNameAsync(updateDto.Name);
            if (existingRole != null && existingRole.Id != role.Id)
            {
                return ServiceResult<RoleResponseDTO>.Failure("Another role with this name already exists");
            }

            role.Name = updateDto.Name;
            role.Description = updateDto.Description;
            role.IsActive = updateDto.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to update role: {Errors}", errors);
                return ServiceResult<RoleResponseDTO>.Failure($"Failed to update role: {errors}");
            }

            var roleDto = new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedBy = role.UpdatedBy
            };

            return ServiceResult<RoleResponseDTO>.Success(roleDto, "Role updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role with ID {RoleId}", id);
            return ServiceResult<RoleResponseDTO>.Failure("Failed to update role");
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return ServiceResult<bool>.Failure("Role not found");
            }

            // Check if any users are assigned to this role
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Any())
            {
                return ServiceResult<bool>.Failure("Cannot delete role that has users assigned to it");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to delete role: {Errors}", errors);
                return ServiceResult<bool>.Failure($"Failed to delete role: {errors}");
            }

            return ServiceResult<bool>.Success(true, "Role deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role with ID {RoleId}", id);
            return ServiceResult<bool>.Failure("Failed to delete role");
        }
    }

    #endregion

    #region Soft Delete Methods

    public async Task<ServiceResult<bool>> SoftDeleteAsync(Guid id)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return ServiceResult<bool>.Failure("Role not found");
            }

            // Soft delete by setting IsActive = false
            role.IsActive = false;
            role.UpdatedAt = DateTime.UtcNow;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to soft delete role: {Errors}", errors);
                return ServiceResult<bool>.Failure($"Failed to soft delete role: {errors}");
            }

            _logger.LogInformation("Role {RoleId} soft deleted successfully", id);
            return ServiceResult<bool>.Success(true, "Role soft deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting role with ID {RoleId}", id);
            return ServiceResult<bool>.Failure("Failed to soft delete role");
        }
    }

    public async Task<ServiceResult<bool>> RestoreAsync(Guid id)
    {
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return ServiceResult<bool>.Failure("Role not found");
            }

            // Restore by setting IsActive = true
            role.IsActive = true;
            role.UpdatedAt = DateTime.UtcNow;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to restore role: {Errors}", errors);
                return ServiceResult<bool>.Failure($"Failed to restore role: {errors}");
            }

            _logger.LogInformation("Role {RoleId} restored successfully", id);
            return ServiceResult<bool>.Success(true, "Role restored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring role with ID {RoleId}", id);
            return ServiceResult<bool>.Failure("Failed to restore role");
        }
    }

    public async Task<ServiceResult<IEnumerable<RoleResponseDTO>>> GetDeletedAsync()
    {
        try
        {
            var deletedRoles = _roleManager.Roles.Where(r => !r.IsActive).ToList();
            var roleDtos = deletedRoles.Select(role => new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedBy = role.UpdatedBy
            }).ToList();

            return ServiceResult<IEnumerable<RoleResponseDTO>>.Success(roleDtos, "Deleted roles retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deleted roles");
            return ServiceResult<IEnumerable<RoleResponseDTO>>.Failure("Failed to retrieve deleted roles");
        }
    }

    #endregion

    #region Additional Methods

    public async Task<ServiceResult<IEnumerable<RoleResponseDTO>>> GetActiveRolesAsync()
    {
        try
        {
            var roles = _roleManager.Roles.Where(r => r.IsActive).ToList();
            var roleDtos = roles.Select(role => new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedBy = role.UpdatedBy
            }).ToList();

            return ServiceResult<IEnumerable<RoleResponseDTO>>.Success(roleDtos, "Active roles retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active roles");
            return ServiceResult<IEnumerable<RoleResponseDTO>>.Failure("Failed to retrieve active roles");
        }
    }

    public async Task<ServiceResult<RoleResponseDTO>> GetByNameAsync(string name)
    {
        try
        {
            var role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                return ServiceResult<RoleResponseDTO>.Failure("Role not found");
            }

            var roleDto = new RoleResponseDTO
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedBy = role.UpdatedBy
            };

            return ServiceResult<RoleResponseDTO>.Success(roleDto, "Role retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with name {RoleName}", name);
            return ServiceResult<RoleResponseDTO>.Failure("Failed to retrieve role");
        }
    }

    #endregion
}
