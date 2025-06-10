using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Jpeg;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Infrastructure.Data;
using TheMagicParents.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using TheMagicParents.Core.Responses;
using TheMagicParents.Enums;
using TheMagicParents.Core.EmailService;

namespace TheMagicParents.Infrastructure.Repositories
{
    public class UserRepository:IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserRepository> _logger;
        private readonly Core.Interfaces.IEmailSender _emailSender;

        public UserRepository(AppDbContext context, UserManager<User> userManager, IConfiguration configuration, ILogger<UserRepository> logger, Core.Interfaces.IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task<IEnumerable<Governorate>> GetGovernorateAsync()
        {
            return await _context.Governorates.ToListAsync();
        }

        public async Task<IEnumerable<City>> GetCitiesByGovernorateAsync(int GovernorateId)
        {
            return await _context.Cities
                .Where(c => c.GovernorateId == GovernorateId)
                .ToListAsync();
        }

        public async Task<string> GenerateUserNameIdFromEmailAsync(string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                return email.Split('@')[0];
            }
            return null;
        }

        public async Task<string> SaveImage(IFormFile image)
        {
            var fileName = Path.GetFileNameWithoutExtension(image.FileName);
            var extension = Path.GetExtension(image.FileName);
            var newFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

            // استخدام مسار نسبي داخل wwwroot
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var filePath = Path.Combine(basePath, newFileName);

            // ضغط الصورة
            using (var imageStream = image.OpenReadStream())
            using (var outputStream = new FileStream(filePath, FileMode.Create))
            using (var img = await Image.LoadAsync(imageStream))
            {
                // تغيير حجم الصورة (اختياري)
                img.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(800, 800), // تغيير الحجم إلى 800x800
                    Mode = ResizeMode.Max // الحفاظ على نسبة العرض إلى الارتفاع
                }));

                // ضغط الصورة
                var encoder = new JpegEncoder
                {
                    Quality = 75 // جودة الضغط (من 0 إلى 100)
                };

                await img.SaveAsync(outputStream, encoder);
            }

            return $"/images/{newFileName}";
        }

        public async Task<(JwtSecurityToken Token, DateTime Expires)> GenerateJwtToken<TUser>(TUser user) where TUser : User
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // إضافة الأدوار
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return (token, expires);
        }

        public async Task<bool> SubmitReportAsync(string reporterUserId, string reportedUserNameId, string comment)
            {
                var reportedUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == reportedUserNameId);

                if (reportedUser == null || reporterUserId == reportedUser.Id)
                    return false;

                var support = new Support
                {
                    Comment = comment,
                    Status = "Pending",
                    ComplainerId = reporterUserId,
                    user = reportedUser
                };

                _context.Supports.Add(support);
                await _context.SaveChangesAsync();
                return true;
            }

        public async Task<IEnumerable<Support>> GetPendingReportsAsync()
            {
                return await _context.Supports
                    .Include(s => s.user)
                    .Where(s => s.Status == "Pending")
                    .ToListAsync();
            }

        public async Task<bool> HandleReportAsync(int reportId, bool isImportant)
            {
                var report = await _context.Supports
                    .Include(s => s.user)
                    .FirstOrDefaultAsync(s => s.SupportID == reportId);

                if (report == null)
                    return false;

            if (isImportant)
            {
                report.user.NumberOfSupports++;
                report.Status = "Approved";
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Supports.Remove(report);  // Delete the report if rejected
                await _context.SaveChangesAsync();
            }
            return true;

            }

        public async Task<List<BookingStatusRsponse>> GetPendingBookingsAsync(string userId)
        {
            return await GetBookingsByStatusAsync(userId, BookingStatus.pending);
        }

        public async Task<List<BookingStatusRsponse>> GetProviderConfirmedBookingsAsync(string userId)
        {
            return await GetBookingsByStatusAsync(userId, BookingStatus.provider_confirmed);
        }

        public async Task<List<BookingStatusRsponse>> GetPaidBookingsAsync(string userId)
        {
            return await GetBookingsByStatusAsync(userId, BookingStatus.paid);
        }

        public async Task<List<BookingStatusRsponse>> GetCancelledBookingsAsync(string userId)
        {
            return await GetBookingsByStatusAsync(userId, BookingStatus.cancelled);
        }

        public async Task<List<BookingStatusRsponse>> GetCompletedBookingsAsync(string userId)
        {
            return await GetBookingsByStatusAsync(userId, BookingStatus.completed);
        }

        public async Task<List<BookingStatusRsponse>> GetRejectedBookingsAsync(string userId)
        {
            return await GetBookingsByStatusAsync(userId, BookingStatus.rejected);
        }

        private async Task<List<BookingStatusRsponse>> GetBookingsByStatusAsync(string userId, BookingStatus status)
        {
            try
            {
                var serviceProviderRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "ServiceProvider");

                bool isProvider = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == userId && ur.RoleId == serviceProviderRole.Id);

                IQueryable<Booking> query = _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.ServiceProvider)
                    .Where(b => b.Status == status);

                query = isProvider
                    ? query.Where(b => b.ServiceProviderID == userId)
                    : query.Where(b => b.ClientId == userId);

                return await query
                    .OrderByDescending(b => b.Day)
                    .ThenByDescending(b => b.Houre)
                    .Select(b => new BookingStatusRsponse
                    {
                        BookingID = b.BookingID,
                        ClientId = b.ClientId,
                        ServiceProviderID = b.ServiceProviderID,
                        Day = b.Day,
                        Hours = b.Houre,
                        Location = b.Location,
                        Status = b.Status,
                        TotalPrice = b.TotalPrice,
                        ClientName=b.Client.UserNameId,
                        ServiceProviderName=b.ServiceProvider.UserNameId,
                        ServiceType=b.ServiceProvider.Type
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting {status} bookings for user {userId}");
                throw;
            }
        }

        public async Task<CancelBookingResponse> CancelBookingAsync(int bookingId, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = _context.Users.FirstOrDefault(u=>u.Id==userId);
                // 1. Get the booking with all related data
                var booking = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.ServiceProvider)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId);

                if (booking == null || user==null)
                {
                    throw new InvalidOperationException("Booking or user not found.");
                }

                var bookingDateTime = booking.Day.Add(booking.Houre);
                var timeUntilBooking = bookingDateTime - DateTime.Now;

                if (timeUntilBooking <= TimeSpan.FromHours(1) && booking.Status==BookingStatus.provider_confirmed)
                {
                    throw new InvalidOperationException("Confirmed bookings can only be cancelled at least 1 hour before the scheduled time.");
                }

                // 3. Validate booking can be cancelled (only in pending or provider_confirmed states)
                if (booking.Status != BookingStatus.pending && booking.Status != BookingStatus.provider_confirmed)
                {
                    throw new InvalidOperationException($"Booking cannot be cancelled in its current state ({booking.Status})");
                }

                // 4. Update booking status
                booking.Status = BookingStatus.cancelled;
                booking.cancelledBy = userId;
                _context.Bookings.Update(booking);

                await _context.SaveChangesAsync();

                bool isClient = booking.ClientId == userId;
                bool isProvider = booking.ServiceProviderID == userId;

                // 6. Send appropriate notification
                try
                {
                    if (isClient)
                    {
                        await SendCancellationNotificationAsync(
                            recipientEmail: booking.ServiceProvider.Email,
                            cancelledByName: $"{booking.Client.UserNameId}",
                            booking: booking
                        );
                    }
                    else
                    {
                        await SendCancellationNotificationAsync(
                            recipientEmail: booking.Client.Email,
                            cancelledByName: $"{booking.ServiceProvider.UserNameId}",
                            booking: booking
                        );
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send cancellation email");
                    // Don't fail the operation if email fails
                }

                await transaction.CommitAsync();

                return new CancelBookingResponse
                {
                    BookingId = booking.BookingID,
                    CancelledBy=booking.cancelledBy,
                    NewStatus=booking.Status
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling booking");
                throw;
            }
        }

        private async Task SendCancellationNotificationAsync(string recipientEmail, string cancelledByName, Booking booking)
        {
            try
            {
                string formattedDate = booking.Day.ToString("dddd, MMMM dd, yyyy");

                // تحويل TimeSpan إلى DateTime عشان نقدر نستخدم AM/PM
                DateTime timeAsDateTime = DateTime.Today.Add(booking.Houre);
                string formattedTime = timeAsDateTime.ToString("h:mm tt");

                var subject = "Your booking has been cancelled!";
                var body = $@"
<h2 style='font-family: Arial, sans-serif;'>Booking Cancellation Notification</h2>
<p style='font-family: Arial, sans-serif;'>
    The following booking has been cancelled by {cancelledByName}:
</p>
<ul style='font-family: Arial, sans-serif;'>
    <li><strong>Original Date:</strong> {formattedDate}</li>
    <li><strong>Time:</strong> {formattedTime}</li>
    <li><strong>Location:</strong> {booking.Location}</li>
</ul>
<p style='font-family: Arial, sans-serif;'>
    Please log in to your account for more details.
</p>";

                var mailMessage = new Message(
                    new [] { recipientEmail },
                    subject,
                    body
                );


                await _emailSender.SendEmailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log the email sending error
                throw new InvalidOperationException($"Failed to send cancellation email: {ex.Message}");
            }
        }

    }
}
