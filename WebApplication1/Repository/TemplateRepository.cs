using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class TemplateRepository : ITemplateRepository
    {
        private readonly IMemoryCache _cache;
        private readonly IHostEnvironment _hostEnvironment;

        public TemplateRepository(IMemoryCache cache, IHostEnvironment hostEnvironment)
        {
            _cache = cache;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<string> GetTemplateAsync(string templateName)
        {
            // Use caching to avoid reading the file repeatedly
            if (_cache.TryGetValue(templateName, out string templateContent))
            {
                return templateContent;
            }

            var filePath = Path.Combine(_hostEnvironment.ContentRootPath,"Template", $"{templateName}");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Template file '{filePath}' not found.");
            }

            templateContent = await File.ReadAllTextAsync(filePath);
            _cache.Set(templateName, templateContent, TimeSpan.FromMinutes(30)); // Cache for 30 minutes
            return templateContent;
        }
    }
}
