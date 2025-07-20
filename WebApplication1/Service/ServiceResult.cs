using Microsoft.Extensions.Logging;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Service
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        
        public ServiceResult()
        {
        }
        
        public ServiceResult(bool success, string message, T? data = default)
        {
            IsSuccess = success;
            Message = message ?? string.Empty;
            Data = data;
        }
        
        public static ServiceResult<T> Success(T data, string message = "Operation successful")
            => new ServiceResult<T>(true, message ?? "Operation successful", data);
        
        public static ServiceResult<T> Failure(string message)
            => new ServiceResult<T>(false, message ?? "Operation failed", default);

        /// <summary>
        /// Executes an operation with standardized error handling and logging
        /// </summary>
        public static async Task<ServiceResult<TResult>> ExecuteWithErrorHandlingAsync<TResult>(
            Func<Task<ServiceResult<TResult>>> operation,
            IUnitOfWork unitOfWork,
            ILogger logger,
            string errorMessage)
        {
            try
            {
                var result = await operation();
                
                if (result.IsSuccess)
                {
                    await unitOfWork.SaveChangesAsync();
                }
                
                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                return ServiceResult<TResult>.Failure($"Error: {e.Message}");
            }
        }

        /// <summary>
        /// Executes an operation with transaction management and error handling
        /// </summary>
        public static async Task<ServiceResult<TResult>> ExecuteWithTransactionAsync<TResult>(
            Func<Task<ServiceResult<TResult>>> operation,
            IUnitOfWork unitOfWork,
            ILogger logger,
            string errorMessage)
        {
            try
            {
                await unitOfWork.BeginTransactionAsync();
                var result = await operation();
                
                if (result.IsSuccess)
                {
                    await unitOfWork.SaveChangesAsync();
                    await unitOfWork.CommitTransactionAsync();
                }
                else
                {
                    await unitOfWork.RollbackTransactionAsync();
                }
                
                return result;
            }
            catch (Exception e)
            {
                await unitOfWork.RollbackTransactionAsync();
                logger.LogError(e, "{ErrorMessage}", errorMessage);
                return ServiceResult<TResult>.Failure($"Error: {e.Message}");
            }
        }

        
    }
}
