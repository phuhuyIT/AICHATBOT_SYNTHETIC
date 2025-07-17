using WebApplication1.DTO;

namespace WebApplication1.Service.Interface
{
    /// <summary>
    /// Read-only CRUD operations returning response DTOs.
    /// </summary>
    /// <typeparam name="TResponseDto">DTO returned to callers.</typeparam>
    public interface IReadService<TResponseDto>
    {
        Task<ServiceResult<IEnumerable<TResponseDto>>> GetAllAsync();
        Task<ServiceResult<TResponseDto>> GetByIdAsync(int id);
    }
}
