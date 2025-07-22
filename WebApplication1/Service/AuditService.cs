using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface;

public interface IAuditService
{
    string? GetCurrentUserId();
    Task SetAuditFieldsForCreate(AuditableEntity entity);
    Task SetAuditFieldsForUpdate(AuditableEntity entity);
}

public class AuditService : IAuditService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<User> _userManager;
    private readonly IDatabaseSeederService _seederService;

    public AuditService(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager, IDatabaseSeederService seederService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _seederService = seederService;
    }

    public string? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        return null;
    }

    public async Task SetAuditFieldsForCreate(AuditableEntity entity)
    {
        var currentUserId = GetCurrentUserId();
        entity.CreatedBy = currentUserId ?? await _seederService.GetSystemUserIdAsync();
        entity.CreatedAt = DateTime.UtcNow;
    }

    public async Task SetAuditFieldsForUpdate(AuditableEntity entity)
    {
        var currentUserId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(currentUserId))
        {
            entity.UpdatedBy = currentUserId;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
