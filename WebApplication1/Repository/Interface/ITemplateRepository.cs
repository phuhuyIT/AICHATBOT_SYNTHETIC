namespace WebApplication1.Repository.Interface
{
    public interface ITemplateRepository
    {
        Task<string> GetTemplateAsync(string templateName);
        Task<bool> TemplateExistsAsync(string templateName);
        void ClearTemplateCache(string templateName);
        void ClearAllTemplateCache();
    }
}
