namespace WebApplication1.Service.Interface
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task<string> RenderTemplateAsync(string templateName, IDictionary<string, string> tokens);
    }
}
