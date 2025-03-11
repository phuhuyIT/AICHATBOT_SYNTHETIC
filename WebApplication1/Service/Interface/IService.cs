namespace WebApplication1.Service.Interface
{
    public interface IService<T> where T : class
    {
        // crud
        Task<string> AddAsync(T entity);
        Task<string> DeleteAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task<string> UpdateAsync(T entity);
    }
}
