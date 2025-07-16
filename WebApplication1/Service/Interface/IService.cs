using WebApplication1.DTO;

namespace WebApplication1.Service.Interface
{
    public interface IService<T> where T : class
    {
        // crud
        Task<ServiceResult<T>> AddAsync(T entity);
        Task<ServiceResult<bool>> DeleteAsync(int id);
        Task<ServiceResult<IEnumerable<T>>> GetAllAsync();
        Task<ServiceResult<T>> GetByIdAsync(int id);
        Task<ServiceResult<T>> UpdateAsync(T entity);
    }
}
