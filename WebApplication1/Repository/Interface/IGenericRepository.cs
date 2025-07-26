using System.Linq.Expressions;

namespace WebApplication1.Repository.Interface
{
    public interface IGenericRepository<T> where T : class
    {
        // CRUD method
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task<bool> DeleteAsync(Guid id);
        Task<T?> FindAsync(Expression<Func<T, bool>> match);
        
        // Soft delete methods
        Task<bool> SoftDeleteAsync(Guid id);
        Task<bool> RestoreAsync(Guid id);
        
        // Admin methods to include soft-deleted entities
        Task<IEnumerable<T>> GetAllIncludingDeletedAsync();
        Task<T?> GetByIdIncludingDeletedAsync(Guid id);
        
    }
}
