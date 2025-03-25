using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheMagicParents.Infrastructure.Data;
using TheMagicParents.Models;

namespace EcommerceMola.EmailModels
{
    public class UserCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);

        public UserCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupExpiredUsersAsync();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }

        private async Task CleanupExpiredUsersAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Fetch users whose emails are not confirmed
                var users = await dbContext.Users.Where(u => !u.EmailConfirmed)
                    .ToListAsync();

                // Filter users whose creation date is older than 24 hours
                var usersToDelete = users.Where(u => (DateTime.Now - u.CreatedAt) > TimeSpan.FromHours(24)).ToList();

                foreach (var user in usersToDelete)
                {
                    await userManager.DeleteAsync(user);
                }
            }
        }
    }
}