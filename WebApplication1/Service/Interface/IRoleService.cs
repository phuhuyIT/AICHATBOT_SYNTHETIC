using WebApplication1.DTO;
using WebApplication1.DTO.Role;

namespace WebApplication1.Service.Interface
{
    public interface IRoleService : IReadService<RoleResponseDTO>, IWriteService<RoleCreateDTO, RoleUpdateDTO, RoleResponseDTO>
    {
        Task<ServiceResult<IEnumerable<RoleResponseDTO>>> GetActiveRolesAsync();
        Task<ServiceResult<RoleResponseDTO>> GetByNameAsync(string name);
    }
}
