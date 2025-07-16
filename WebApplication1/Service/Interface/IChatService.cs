using WebApplication1.DTO;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IChatService
    {
        Task<ServiceResult<Message>> SendMessageAsync(string userId, int conversationId, string userMessage, string modelName);
        Task<ServiceResult<string>> GetAIResponseAsync(string userMessage, string modelName, List<Message> conversationHistory);
        Task<ServiceResult<IEnumerable<Message>>> GetConversationMessagesAsync(int conversationId, string userId);
        Task<ServiceResult<IEnumerable<Message>>> GetPaginatedMessagesAsync(int conversationId, string userId, int pageNumber, int pageSize);
        Task<ServiceResult<bool>> DeleteMessageAsync(int messageId, string userId);
        Task<ServiceResult<Message>> RegenerateResponseAsync(int messageId, string userId, string? newModelName = null);
        Task<ServiceResult<IEnumerable<string>>> GetAvailableModelsAsync(bool isPaidUser);
    }
}
