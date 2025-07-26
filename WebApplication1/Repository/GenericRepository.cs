using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using WebApplication1.Data;
using WebApplication1.Models;
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

        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            // Removed SaveChangesAsync - will be handled by Unit of Work
        }

        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            // Removed SaveChangesAsync - will be handled by Unit of Work
            return true;
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            var query = _dbSet.AsNoTracking();
            
            // For other entities, filter by IsActive
            var isActiveProperty = typeof(T).GetProperty("IsActive");
            if (isActiveProperty != null)
            {
                return await query.Where(e => (bool)isActiveProperty.GetValue(e)).ToListAsync();
            }

            // If no IsActive property, return all
            return await query.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetAllActiveAsync()
        {
            var query = _dbSet.AsNoTracking();

            // For entities with IsActive property, filter by IsActive
            var isActiveProperty = typeof(T).GetProperty("IsActive");
            if (isActiveProperty != null)
            {
                return await query.Where(e => (bool)isActiveProperty.GetValue(e)).ToListAsync();
            }

            // If no IsActive property, return all
            return await query.ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
                return null;

            // For entities with IsActive property, check if active
            var isActiveProperty = typeof(T).GetProperty("IsActive");
            if (isActiveProperty != null)
            {
                var isActive = (bool)isActiveProperty.GetValue(entity);
                return isActive ? entity : null;
            }

            return entity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
        }

        // Soft delete methods
        public virtual async Task<bool> SoftDeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
                return false;
            
            // For entities with IsActive property, set IsActive = false
            var isActiveProperty = typeof(T).GetProperty("IsActive");
            if (isActiveProperty != null && isActiveProperty.CanWrite)
            {
                isActiveProperty.SetValue(entity, false);
                return true;
            }

            return false; // Entity doesn't support soft delete
        }

        public virtual async Task<bool> RestoreAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
                return false;

            // For entities with IsActive property, set IsActive = true
            var isActiveProperty = typeof(T).GetProperty("IsActive");
            if (isActiveProperty != null && isActiveProperty.CanWrite)
            {
                isActiveProperty.SetValue(entity, true);
                return true;
            }

            return false; // Entity doesn't support soft delete
        }

        // Methods to get all entities including soft-deleted ones (for admin purposes)
        public virtual async Task<IEnumerable<T>> GetAllIncludingDeletedAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<T?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }
    }
}
