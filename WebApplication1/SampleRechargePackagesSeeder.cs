using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Service
{
    public class RechargePackageSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RechargePackageSeeder> _logger;

        public RechargePackageSeeder(ApplicationDbContext context, ILogger<RechargePackageSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedRechargePackagesAsync()
        {
            if (!await _context.RechargePackages.AnyAsync())
            {
                var packages = new List<RechargePackage>
                {
                    new RechargePackage
                    {
                        Name = "Starter Pack",
                        Description = "Perfect for beginners",
                        Amount = 10.00m,
                        BonusAmount = 1.00m,
                        IsActive = true,
                        SortOrder = 1
                    },
                    new RechargePackage
                    {
                        Name = "Popular Pack",
                        Description = "Most popular choice",
                        Amount = 25.00m,
                        BonusAmount = 5.00m,
                        IsActive = true,
                        SortOrder = 2
                    },
                    new RechargePackage
                    {
                        Name = "Premium Pack",
                        Description = "Best value for money",
                        Amount = 50.00m,
                        BonusAmount = 15.00m,
                        IsActive = true,
                        SortOrder = 3
                    },
                    new RechargePackage
                    {
                        Name = "Holiday Special",
                        Description = "Limited time offer",
                        Amount = 20.00m,
                        BonusAmount = 10.00m,
                        IsPromotional = true,
                        PromotionStartDate = DateTime.UtcNow,
                        PromotionEndDate = DateTime.UtcNow.AddDays(30),
                        IsActive = true,
                        SortOrder = 4
                    }
                };

                await _context.RechargePackages.AddRangeAsync(packages);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Recharge packages seeded successfully");
            }
        }
    }
}
