using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Service.Interface;
using WebApplication1.DTO.Role;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleController> _logger;

    public RoleController(IRoleService roleService, ILogger<RoleController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    #region CRUD Operations

    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        try
        {
            var result = await _roleService.GetAllAsync();
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllRoles");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoleById(Guid id)
    {
        try
        {
            var result = await _roleService.GetByIdAsync(id);
            
            if (result.IsSuccess)
                return Ok(new { success = true, data = result.Data, message = result.Message });
            
            return NotFound(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRoleById for ID {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] RoleCreateDTO roleDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
            }

            var result = await _roleService.CreateAsync(roleDto);
            
            if (result.IsSuccess && result.Data != null)
            {
                return CreatedAtAction(nameof(GetRoleById), new { id = result.Data.Id }, 
                    new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateRole");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] RoleUpdateDTO roleDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
            }

            if (id.ToString() != roleDto.Id)
            {
                return BadRequest(new { success = false, message = "ID mismatch" });
            }

            var result = await _roleService.UpdateAsync(id, roleDto);
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateRole for ID {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        try
        {
            var result = await _roleService.DeleteAsync(id);
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, message = result.Message });
            }
            
            return NotFound(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteRole for ID {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region Soft Delete Operations

    [HttpDelete("{id}/soft")]
    public async Task<IActionResult> SoftDeleteRole(Guid id)
    {
        try
        {
            var result = await _roleService.SoftDeleteAsync(id);
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, message = result.Message });
            }
            
            return NotFound(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SoftDeleteRole for ID {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPatch("{id}/restore")]
    public async Task<IActionResult> RestoreRole(Guid id)
    {
        try
        {
            var result = await _roleService.RestoreAsync(id);
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, message = result.Message });
            }
            
            return NotFound(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RestoreRole for ID {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedRoles()
    {
        try
        {
            var result = await _roleService.GetDeletedAsync();
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDeletedRoles");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region Additional Operations

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveRoles()
    {
        try
        {
            var result = await _roleService.GetActiveRolesAsync();
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetActiveRoles");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("name/{name}")]
    public async Task<IActionResult> GetRoleByName(string name)
    {
        try
        {
            var result = await _roleService.GetByNameAsync(name);
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return NotFound(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRoleByName for name {Name}", name);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    #endregion
}
