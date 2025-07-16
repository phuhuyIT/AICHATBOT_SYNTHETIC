using WebApplication1.DTO;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IChatbotModelsService : IService<ChatbotModel>
    {
        Task<ServiceResult<ChatbotModel>> AddWithApiKeysAsync(ChatbotModelCreateDTO dto);
        Task<ServiceResult<ChatbotModel>> UpdateWithApiKeysAsync(ChatbotModelUpdateDTO dto);
    }
}

