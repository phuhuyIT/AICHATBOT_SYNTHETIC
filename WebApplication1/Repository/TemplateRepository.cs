using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class TemplateRepository : ITemplateRepository
    {
        private readonly IMemoryCache _cache;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<TemplateRepository> _logger;
        private readonly MemoryCacheEntryOptions _cacheOptions;

        public TemplateRepository(IMemoryCache cache, IHostEnvironment hostEnvironment, ILogger<TemplateRepository> logger)
        {
            _cache = cache;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(15),
                Priority = CacheItemPriority.Normal
            };
        }

        public async Task<string> GetTemplateAsync(string templateName)
        {
            var cacheKey = $"template_{templateName}";
            
            // Use caching to avoid reading the file repeatedly
            if (_cache.TryGetValue(cacheKey, out string? templateContent) && templateContent != null)
            {
                return templateContent;
            }

            try
            {
                var filePath = Path.Combine(_hostEnvironment.ContentRootPath, "Template", templateName);
                if (!File.Exists(filePath))
                {
                    _logger.LogError("Template file '{FilePath}' not found", filePath);
                    throw new FileNotFoundException($"Template file '{filePath}' not found.");
                }

                templateContent = await File.ReadAllTextAsync(filePath);
                
                // Cache the template content
                _cache.Set(cacheKey, templateContent, _cacheOptions);
                
                _logger.LogInformation("Template '{TemplateName}' loaded and cached successfully", templateName);
                return templateContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template '{TemplateName}'", templateName);
                throw;
            }
        }

        public async Task<bool> TemplateExistsAsync(string templateName)
        {
            var filePath = Path.Combine(_hostEnvironment.ContentRootPath, "Template", templateName);
            return File.Exists(filePath);
        }

        public void ClearTemplateCache(string templateName)
        {
            var cacheKey = $"template_{templateName}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Cache cleared for template '{TemplateName}'", templateName);
        }

        public void ClearAllTemplateCache()
        {
            // Note: This is a simplified approach. In production, you might want to track cache keys
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Clear();
                _logger.LogInformation("All template cache cleared");
            }
        }
    }
}
