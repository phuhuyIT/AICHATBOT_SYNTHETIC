using WebApplication1.DTO;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IChatbotModelsService : IReadService<ChatbotModelResponseDTO>, IWriteService<ChatbotModelCreateDTO, ChatbotModelUpdateDTO, ChatbotModelResponseDTO>
    {
        public Task<ServiceResult<ChatbotModelResponseDTO>> GetPaidChatbotModel();
    }
}
