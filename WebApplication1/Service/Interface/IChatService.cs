using WebApplication1.DTO;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IChatService
    {
        Task<ServiceResult<Message>> SendMessageAsync(string userId, Guid conversationId, string userMessage, string modelName);
        Task<ServiceResult<string>> GetAIResponseAsync(string userMessage, string modelName, List<Message> conversationHistory);
        Task<ServiceResult<IEnumerable<Message>>> GetConversationMessagesAsync(Guid conversationId, string userId);
        Task<ServiceResult<IEnumerable<Message>>> GetPaginatedMessagesAsync(Guid branchId, string userId, int pageNumber, int pageSize);
        Task<ServiceResult<bool>> DeleteMessageAsync(Guid messageId, string userId);
        Task<ServiceResult<Message>> RegenerateResponseAsync(Guid messageId, string userId, string? newModelName = null);
        Task<ServiceResult<IEnumerable<string>>> GetAvailableModelsAsync(bool isPaidUser);
    }
}
