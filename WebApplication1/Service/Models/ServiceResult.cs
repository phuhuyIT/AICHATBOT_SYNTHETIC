namespace WebApplication1.Service.Models
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
    }
}
