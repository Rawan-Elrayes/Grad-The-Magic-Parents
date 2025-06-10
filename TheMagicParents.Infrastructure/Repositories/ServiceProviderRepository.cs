using Microsoft.AspNetCore.Identity;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Infrastructure.Data;
using TheMagicParents.Models;
using TheMagicParents.Enums;
using TheMagicParents.Core.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Data;
using Microsoft.AspNetCore.Http;
using TheMagicParents.Core.Responses;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.Extensions.Logging;
using TheMagicParents.Core.EmailService;
using System.Globalization;
//using System.Linq.Dynamic.Core;


namespace TheMagicParents.Infrastructure.Repositories
{
    public class ServiceProviderRepository : IServiceProviderRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ServiceProviderRepository> _logger;
        private readonly Core.Interfaces.IEmailSender _emailSender;

        public ServiceProviderRepository(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUserRepository userRepository, ILogger<ServiceProviderRepository> logger, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task<ServiceProviderRegisterResponse> RegisterServiceProviderAsync(ServiceProviderRegisterDTO model)
        {
            // التحقق من وجود البريد الإلكتروني مسبقًا
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var ServiceProvider = new ServiceProvider
            {
                UserNameId = model.UserNameId,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                PersonalPhoto = await _userRepository.SaveImage(model.PersonalPhoto),
                IdCardFrontPhoto = await _userRepository.SaveImage(model.IdCardFrontPhoto),
                IdCardBackPhoto = await _userRepository.SaveImage(model.IdCardBackPhoto),
                PersonWithCard = await _userRepository.SaveImage(model.PersonWithCard),
                CityId = model.CityId,
                AccountState = StateType.Waiting,
                Certification = await SavePdf(model.Certification),
                PasswordHash = model.Password,
                Type = model.Type,
                HourPrice = model.HourPrice,
                EmailConfirmed = false
            };
            // توليد UserNameId من البريد الإلكتروني
            ServiceProvider.UserName = await _userRepository.GenerateUserNameIdFromEmailAsync(ServiceProvider.Email);

            // إنشاء المستخدم
            var result = await _userManager.CreateAsync(ServiceProvider, model.Password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // التحقق من وجود دور Customer وإنشائه إذا لم يكن موجودًا
            if (!await _roleManager.RoleExistsAsync(UserRoles.ServiceProvider.ToString()))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.ServiceProvider.ToString()));
            }
            await _userManager.AddToRoleAsync(ServiceProvider, UserRoles.ServiceProvider.ToString());

            await _context.SaveChangesAsync();

            var city = _context.Cities
    .Include(c => c.Governorate)
    .FirstOrDefault(c => c.Id == ServiceProvider.CityId);


            return new ServiceProviderRegisterResponse
            {
                Id = ServiceProvider.Id,
                City = city?.Name,
                Government = city?.Governorate?.Name,
                Email = ServiceProvider.Email,
                IdCardBackPhoto = ServiceProvider.IdCardBackPhoto,
                IdCardFrontPhoto = ServiceProvider.IdCardFrontPhoto,
                Certification = ServiceProvider.Certification,
                PersonalPhoto = ServiceProvider.PersonalPhoto,
                PhoneNumber = ServiceProvider.PhoneNumber,
                UserNameId = ServiceProvider.UserNameId,
                UserName = ServiceProvider.UserName
            };
        }

        private async Task<string> SavePdf(IFormFile pdfFile)
        {
            if (pdfFile == null)
            {
                return null;
            }
            // التحقق من أن الملف موجود وصالح
            if (pdfFile == null || pdfFile.Length == 0)
            {
                throw new ArgumentException("The uploaded file is invalid or empty.");
            }

            // التحقق من أن الملف هو PDF
            var extension = Path.GetExtension(pdfFile.FileName).ToLower();
            if (extension != ".pdf")
            {
                throw new ArgumentException("The file must be in PDF format.");
            }

            // إنشاء اسم فريد للملف
            var fileName = Path.GetFileNameWithoutExtension(pdfFile.FileName);
            var newFileName = $"{fileName}_{Guid.NewGuid()}{extension}";

            // استخدام مسار نسبي داخل wwwroot
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var filePath = Path.Combine(basePath, newFileName);

            // حفظ الملف على الخادم
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(fileStream);
            }

            // إرجاع المسار النسبي الذي يمكن استخدامه في الواجهة الأمامية
            return $"/pdfs/{newFileName}";
        }

        public async Task<AvailabilityResponse> SaveAvailability(AvailabilityDTO request, string Id)
        {
            try
            {
                // حذف جميع المواعيد الموجودة لهذا اليوم والبروفايدر
                var existingAvailabilities = await _context.Availabilities
                    .Where(a => a.ServiceProciderID == Id && a.Date == request.Date.Date)
                    .ToListAsync();
                
                if (existingAvailabilities.Any())
                {
                    _context.Availabilities.RemoveRange(existingAvailabilities);
                    await _context.SaveChangesAsync();
                }
        
                // إذا كانت الساعات null أو فارغة، احذف اليوم كله واحفظ التغييرات
                if (request.Hours == null || !request.Hours.Any())
                {
                    await _context.SaveChangesAsync();
                    return new AvailabilityResponse
                    {
                        Date = request.Date.Date,
                        Hours = new List<TimeSpan>()
                    };
                }
                
                // إنشاء المواعيد الجديدة
                var newAvailabilities = new List<Availability>();
                foreach (var hour in request.Hours)
                {
                    newAvailabilities.Add(new Availability
                    {
                        ServiceProciderID = Id,
                        Date = request.Date.Date,
                        StartTime = hour,
                        EndTime = hour.Add(TimeSpan.FromHours(1))
                    });
                }
                
                // إضافة المواعيد الجديدة
                await _context.Availabilities.AddRangeAsync(newAvailabilities);
                await _context.SaveChangesAsync();
                
                return new AvailabilityResponse
                {
                    Date = request.Date.Date,
                    Hours = newAvailabilities.Select(a => a.StartTime).OrderBy(h => h).ToList()
                };
            }
            catch (Exception ex)
            {
                // Log error here
                throw new InvalidOperationException($"An error occurred while saving availability: {ex.Message}");
            }
        }

        public async Task<AvailabilityResponse> GetAvailabilitiesHoures(DateTime date, string Id)
        {
            try
            {
                var availabilities = await GetByDateAsync(date, Id);

                return new AvailabilityResponse
                {
                    Date = date,
                    Hours = availabilities
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while getting availabilities.");
            }
        }

        private async Task<List<TimeSpan>> GetByDateAsync(DateTime date, string Id)
        {
            return await _context.Availabilities
                .Where(a => a.Date.Date == date.Date.Date && a.ServiceProciderID == Id).Select(a=>a.StartTime)
                .ToListAsync();
        }

        public async Task<ProviderGetDataResponse> GetProfileAsync(string userId)
        {
            var provider = await _userManager.Users.OfType<ServiceProvider>()
        .Where(p => p.Id == userId)
        .Select(p => new
        {
            p.UserNameId,
            p.PhoneNumber,
            p.PersonalPhoto,
            p.HourPrice,
            p.Type,
            p.CityId,
            CityName = p.City.Name,
            GovernmentId = p.City.GovernorateId,
            GovernmentName = p.City.Governorate.Name
        })
        .FirstOrDefaultAsync();

            if (provider == null)
                throw new InvalidOperationException("Service provider not found");

            return new ProviderGetDataResponse
            {
                UserNameId = provider.UserNameId,
                PhoneNumber = provider.PhoneNumber,
                PersonalPhoto = provider.PersonalPhoto,
                GovernmentId = provider.GovernmentId,
                Government = provider.GovernmentName,
                CityId = provider.CityId ?? 0,
                City = provider.CityName,
                HourPrice = provider.HourPrice,
            };
        }

        public async Task<ProviderGetDataResponse> UpdateProfileAsync(string userId, ServiceProviderUpdateProfileDTO model)
        {
            var provider = await _userManager.Users.OfType<ServiceProvider>()
            .Include(p => p.City)
            .ThenInclude(city => city.Governorate)
            .FirstOrDefaultAsync(p => p.Id == userId);

            if (provider == null)
                throw new InvalidOperationException("Service provider not found");

            provider.UserNameId = model.UserNameId;
            provider.PhoneNumber = model.PhoneNumber;
            provider.CityId = model.CityId;
            provider.HourPrice = model.HourPrice;

            if (model.PersonalPhoto != null)
            {
                provider.PersonalPhoto = await _userRepository.SaveImage(model.PersonalPhoto);
            }

            var result = await _userManager.UpdateAsync(provider);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _context.SaveChangesAsync();

            // Reload to get updated data with relationships
            var updatedProvider = await _userManager.Users.OfType<ServiceProvider>()
                .Include(p => p.City)
                    .ThenInclude(city => city.Governorate)
                .FirstOrDefaultAsync(p => p.Id == userId);

            return new ProviderGetDataResponse
            {
                UserNameId = updatedProvider.UserNameId,
                PhoneNumber = updatedProvider.PhoneNumber,
                PersonalPhoto = updatedProvider.PersonalPhoto,
                GovernmentId = updatedProvider.City.GovernorateId,
                Government = updatedProvider.City.Governorate.Name,
                CityId = updatedProvider.CityId,
                City = updatedProvider.City.Name,
                HourPrice = updatedProvider.HourPrice
            };
        }

        public async Task<PagedResult<FilteredProviderDTO>> GetFilteredProvidersAsync(ProviderFilterDTO filter, int page = 1, int pageSize = 8)
        {
            var query = _context.ServiceProviders
                .Include(sp => sp.City)
                .Include(sp => sp.Availabilities)
                .Where(sp => sp.Type == filter.ServiceType
                    && sp.CityId == filter.CityId
                    && sp.City.GovernorateId == filter.GovernmentId
                    && sp.AccountState == StateType.Active);

            // Check if provider has any availabilities on the specified date
            query = query.Where(sp => sp.Availabilities.Any(a =>
                a.Date.Date == filter.Date.Date));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sp => new FilteredProviderDTO
                {
                    Id = sp.Id,
                    UserName = sp.UserName,
                    PersonalPhoto = sp.PersonalPhoto,
                    Rating = sp.Rate,
                    PricePerHour = sp.HourPrice,
                    IsAvailableOnDate = true  // If we got here, they have availability on this date
                })
                .ToListAsync();

            return new Core.Responses.PagedResult<FilteredProviderDTO>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<List<DateTime>> GetAvailableDays(string userId)
        {
            try
            {
                var availabilities = await _context.Availabilities
                .Where(a => a.ServiceProciderID == userId).Select(a => a.Date.Date).Distinct()
                .ToListAsync();

                return availabilities;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while getting available days.");
            }
        }

        public async Task<BookingConfirmationResponse> ConfirmBookingAsync(int bookingId, string providerId)
        {
            try
            {
                var result = await UpdateBookingStatusAsync(bookingId, providerId, BookingStatus.provider_confirmed, "confirming");
                var booking = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.ServiceProvider)
                    .FirstAsync(b => b.BookingID == bookingId);

                await SendConfirmationEmail(booking, isConfirmed: true);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email");
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task<BookingConfirmationResponse> RejectBookingAsync(int bookingId, string providerId)
        {
            try
            {
                var result = await UpdateBookingStatusAsync(
                bookingId,
                providerId,
                BookingStatus.rejected,
                "rejecting");

                var booking = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.ServiceProvider)
                    .FirstAsync(b => b.BookingID == bookingId);

                await SendConfirmationEmail(booking, isConfirmed: false);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection email");
                throw new InvalidOperationException(ex.Message);
            }
        }

        private async Task SendConfirmationEmail(Booking booking, bool isConfirmed)
        {
            try
            {
                if (booking == null || booking.Client == null || booking.ServiceProvider == null)
                {
                    _logger.LogError("Booking or related data is null");
                    return;
                }

                // تنسيق الوقت بشكل آمن
                string formattedTime;
                try
                {
                    formattedTime = booking.Houre.ToString(@"hh\:mm tt", CultureInfo.InvariantCulture);
                }
                catch (Exception timeEx)
                {
                    _logger.LogError(timeEx, "Error formatting time");
                    formattedTime = booking.Houre.ToString(); // استخدام التنسيق الافتراضي إذا فشل التنسيق المخصص
                }

                var emailSubject = isConfirmed ? "Your Booking is Confirmed" : "Your Booking is Rejected";

                var emailBody = $@"
<div style='font-family: Arial; text-align: center;'>
    <h2>{(isConfirmed ? "Your Booking is Confirmed" : "Your Booking is Rejected")}</h2>
    <p>Dear {booking.Client.UserName ?? "Customer"},</p>
    
    <p>{(isConfirmed ?
                "We are pleased to inform you that your booking has been successfully confirmed with the service provider." :
                "We regret to inform you that your booking has been rejected by the service provider.")}</p>
    
    <div style='border: 1px solid #ddd; padding: 15px; margin: 15px 0;'>
        <h3>Booking Details</h3>
        <p><strong>Service Provider:</strong> {booking.ServiceProvider.UserName ?? "Service Provider"}</p>
        <p><strong>Date:</strong> {booking.Day:yyyy-MM-dd}</p>
        <p><strong>Time:</strong> {formattedTime}</p>
        <p><strong>Status:</strong> {(isConfirmed ? "Confirmed" : "Rejected")}</p>
    </div>

    <p>{(isConfirmed ?
                "You can contact the service provider for any further details." :
                "You can search for other available dates or service providers.")}</p>
    
    <p>Thank you for using our platform,</p>
    <p>The Magic Parents Team</p>
</div>";

                var message = new Message(
                    new[] { booking.Client.Email },
                    emailSubject,
                    emailBody
                );

                await _emailSender.SendEmailAsync(message);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        private async Task<BookingConfirmationResponse> UpdateBookingStatusAsync(int bookingId, string providerId, BookingStatus newStatus, string successActionName)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // التحقق من وجود الحجز والإذن
                var booking = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.ServiceProvider)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.ServiceProviderID == providerId);

                if (booking == null)
                {
                    throw new ArgumentException("Booking not found or you don't have permission.");
                }

                if (booking.Status != BookingStatus.pending)
                {
                    throw new ArgumentException($"Booking cannot be {successActionName} in current state ({booking.Status})");
                }

                // تحديث الحالة
                booking.Status = newStatus;
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new BookingConfirmationResponse
                {
                    BookingId = bookingId,
                    NewStatus = newStatus
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error {successActionName} booking");
                throw;
            }
        }
    }
}