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
    public class BookingCompletionService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<BookingCompletionService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

        // أضف constructor واضحًا
        public BookingCompletionService(
            IServiceProvider services,
            ILogger<BookingCompletionService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Completion Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.Now;
                    var bookingsToComplete = await dbContext.Bookings
                    .Where(b => b.Status == BookingStatus.ongoing &&
                               b.Day.Date <= now.Date &&
                               b.Houre.Add(TimeSpan.FromHours(1)) <= now.TimeOfDay).ToListAsync();

                    if (bookingsToComplete.Any())
                    {
                        foreach (var booking in bookingsToComplete)
                        {
                            booking.Status = BookingStatus.completed;
                            _logger.LogInformation($"Completed booking {booking.BookingID}");
                        }
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error completing bookings");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
