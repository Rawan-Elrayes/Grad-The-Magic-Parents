using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;
using TheMagicParents.Infrastructure.Data;

namespace TheMagicParents.Infrastructure.Services
{
    public class BookingStatusUpdaterService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<BookingStatusUpdaterService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // يتم الفحص كل دقيقة

        public BookingStatusUpdaterService(
            IServiceProvider services,
            ILogger<BookingStatusUpdaterService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.Now;

                    // الحجوزات التي يجب أن تصبح ongoing
                    var bookingsToUpdate = await dbContext.Bookings
                        .Where(b => b.Status == BookingStatus.provider_confirmed &&
                                   b.Day.Date == now.Date &&
                                   b.Houre <= now.TimeOfDay &&
                                   b.Houre.Add(TimeSpan.FromMinutes(30)) >= now.TimeOfDay) // نافذة 30 دقيقة
                        .ToListAsync();

                    foreach (var booking in bookingsToUpdate)
                    {
                        booking.Status = BookingStatus.ongoing;
                        _logger.LogInformation($"Updated booking {booking.BookingID} to ongoing status");
                    }

                    if (bookingsToUpdate.Any())
                    {
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating booking statuses");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
