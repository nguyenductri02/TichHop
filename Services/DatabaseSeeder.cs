using CeoMemo.Data;
using Microsoft.EntityFrameworkCore;

namespace CeoMemo.Services
{
    public class DatabaseSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void SeedData()
        {
            try
            {
                _logger.LogInformation("Starting database seeding process...");
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        // Seed Human database first
                        var humanDbContext = scope.ServiceProvider.GetRequiredService<HumanDbContext>();
                        humanDbContext.Database.Migrate();
                        HumanDbSeed.Initialize(humanDbContext);
                        _logger.LogInformation("Human database seeded successfully.");
                        
                        // Clear tracking before seeding Payroll database
                        humanDbContext.ChangeTracker.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while seeding the Human database.");
                    }
                    
                    try
                    {
                        // Then seed Payroll database
                        var payrollDbContext = scope.ServiceProvider.GetRequiredService<PayrollDbContext>();
                        payrollDbContext.Database.Migrate();
                        PayrollDbSeed.Initialize(payrollDbContext);
                        _logger.LogInformation("Payroll database seeded successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while seeding the Payroll database.");
                    }
                }
                
                _logger.LogInformation("Database seeding completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the databases.");
            }
        }
    }
}
