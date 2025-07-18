﻿namespace WebApplication1.Repository.Interface
{
    public interface IGenericRepository<T> where T : class
    {
        // CRUD method
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task<bool> DeleteAsync(Guid id);
    }
}
