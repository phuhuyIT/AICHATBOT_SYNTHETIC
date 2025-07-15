using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Repository;

public class ChatbotModelsRepository : GenericRepository<ChatbotModel>
{
    public ChatbotModelsRepository(ApplicationDbContext context) : base(context)
    {
    }
    
}