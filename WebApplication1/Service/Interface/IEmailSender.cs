using WebApplication1.DTO;

namespace WebApplication1.Service.Interface
{
    public interface IEmailSender
    {
        Task<ServiceResult<bool>> SendEmailAsync(string email, string subject, string message);
        Task<ServiceResult<string>> RenderTemplateAsync(string templateName, IDictionary<string, string> tokens);
    }
}
