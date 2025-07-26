using WebApplication1.DTO;

namespace WebApplication1.Service.Interface
{
    /// <summary>
    /// Write-side CRUD operations using DTOs.
    /// </summary>
    /// <typeparam name="TCreateDto">DTO used for create operations.</typeparam>
    /// <typeparam name="TUpdateDto">DTO used for update operations.</typeparam>
    /// <typeparam name="TResponseDto">DTO returned to callers.</typeparam>
    public interface IWriteService<TCreateDto, TUpdateDto, TResponseDto>
    {
        Task<ServiceResult<TResponseDto>> CreateAsync(TCreateDto createDto);
        Task<ServiceResult<TResponseDto>> UpdateAsync(Guid id, TUpdateDto updateDto);
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        
        // Soft delete methods
        Task<ServiceResult<bool>> SoftDeleteAsync(Guid id);
        Task<ServiceResult<bool>> RestoreAsync(Guid id);
        Task<ServiceResult<IEnumerable<TResponseDto>>> GetDeletedAsync();
    }
}
