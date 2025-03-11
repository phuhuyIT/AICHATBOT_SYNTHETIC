namespace WebApplication1.Repository.Interface
{
    public interface IGenericRepository<T> where T : class
    {
        // CRUD method
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<string> AddAsync(T entity);
        Task<string> UpdateAsync(T entity);
        Task<string> DeleteAsync(int id);
    }
}
