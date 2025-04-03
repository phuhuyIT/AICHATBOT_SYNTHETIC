namespace WebApplication1.Repository.Interface
{
    public interface ITemplateRepository
    {
        Task<string> GetTemplateAsync(string templateName);
    }
}
