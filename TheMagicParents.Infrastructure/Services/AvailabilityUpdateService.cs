using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheMagicParents.Infrastructure.Data;
using TheMagicParents.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TheMagicParents.Services
{
    public class AvailabilityUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _updateInterval = TimeSpan.FromHours(24); // تحديث كل 24 ساعة
        private readonly ILogger<AvailabilityUpdateService> _logger;

        public AvailabilityUpdateService(IServiceProvider serviceProvider, ILogger<AvailabilityUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateExpiredAvailabilitiesAsync();
                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        private async Task UpdateExpiredAvailabilitiesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var today = DateTime.Today;

                // الحصول على التواريخ المنتهية (أقدم من اليوم الحالي)
                var expiredAvailabilities = await dbContext.Availabilities
                    .Where(a => a.Date.Date < today)
                    .ToListAsync();

                if (expiredAvailabilities.Any())
                {
                    foreach (var availability in expiredAvailabilities)
                    {
                        // حساب نفس اليوم في الأسبوع التالي
                        var newDate = availability.Date.AddDays(7);

                        // تحديث التاريخ مع الحفاظ على نفس الوقت
                        availability.Date = new DateTime(
                            newDate.Year,
                            newDate.Month,
                            newDate.Day,
                            availability.Date.Hour,
                            availability.Date.Minute,
                            availability.Date.Second);
                    }

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"تم تحديث {expiredAvailabilities.Count} من التواريخ المنتهية إلى الأسبوع التالي");
                }
            }
        }
    }
}