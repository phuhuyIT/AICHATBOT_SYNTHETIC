using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<string> AddAsync(T entity)
        {
            try
            {
                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();
                return "Entity added successfully.";
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                return $"An error occurred: {ex.Message}";
            }
        }

        public async Task<string> DeleteAsync(int id)
        {
            try
            {
                var entity = await _dbSet.FindAsync(id);
                if (entity != null)
                {
                    _dbSet.Remove(entity);
                    await _context.SaveChangesAsync();
                    return "Entity deleted successfully.";
                }
                else
                {
                    return "Entity not found.";
                }
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                return $"An error occurred: {ex.Message}";
            }
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<string> UpdateAsync(T entity)
        {
            try
            {
                _dbSet.Update(entity);
                await _context.SaveChangesAsync();
                return "Entity updated successfully.";
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                return $"An error occurred: {ex.Message}";
            }
        }
    }
}
