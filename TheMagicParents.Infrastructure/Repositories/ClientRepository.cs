using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Infrastructure.Data;
using TheMagicParents.Models;
using TheMagicParents.Enums;
using TheMagicParents.Core.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using Azure.Core;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheMagicParents.Core.EmailService;
using Microsoft.AspNetCore.Http;
using TheMagicParents.Core.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Mail;
using System.Net;
using System.Globalization;

namespace TheMagicParents.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProviderRepository _serviceProviderRepository;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<UserRepository> _logger;

        public ClientRepository(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUserRepository userRepository, IServiceProviderRepository serviceProviderRepository, IEmailSender emailSender, ILogger<UserRepository> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _serviceProviderRepository = serviceProviderRepository;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<ClientRegisterResponse> RegisterClientAsync(ClientRegisterDTO model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var client = new Client
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
                Location = model.Location,
                PasswordHash = model.Password,
                EmailConfirmed=false
            };
            // توليد UserNameId من البريد الإلكتروني
            client.UserName = await _userRepository.GenerateUserNameIdFromEmailAsync(client.Email);

            // إنشاء المستخدم
            var result = await _userManager.CreateAsync(client, model.Password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            if (!await _roleManager.RoleExistsAsync(UserRoles.Client.ToString()))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Client.ToString()));
            }
            await _userManager.AddToRoleAsync(client, UserRoles.Client.ToString());

            await _context.SaveChangesAsync();

            var city = _context.Cities
     .Include(c => c.Governorate)
     .FirstOrDefault(c => c.Id == client.CityId);

            return new ClientRegisterResponse
            {
                Id = client.Id,
                City = city?.Name,
                Government = city?.Governorate?.Name,
                Email = client.Email,
                IdCardBackPhoto = client.IdCardBackPhoto,
                IdCardFrontPhoto = client.IdCardFrontPhoto,
                Location = client.Location,
                PersonalPhoto = client.PersonalPhoto,
                PhoneNumber = client.PhoneNumber,
                UserNameId = client.UserNameId,
                UserName = client.UserName
            };

        }

        public async Task<ClientGetDataResponse> GetProfileAsync(string userId)
        {
            var client = await _userManager.Users.OfType<Client>()
        .Where(c => c.Id == userId)
        .Select(c => new
        {
            c.UserNameId,
            c.PhoneNumber,
            c.PersonalPhoto,
            c.Location,
            c.CityId,
            CityName = c.City.Name,
            GovernmentId = c.City.GovernorateId,
            GovernmentName = c.City.Governorate.Name
        })
        .FirstOrDefaultAsync();

            if (client == null)
                throw new InvalidOperationException("Client not found");

            return new ClientGetDataResponse
            {
                UserNameId = client.UserNameId,
                PhoneNumber = client.PhoneNumber,
                PersonalPhoto = client.PersonalPhoto,
                GovernmentId = client.GovernmentId,
                Government = client.GovernmentName,
                CityId = client.CityId,
                City = client.CityName,
                Location = client.Location
            };
        }

        public async Task<ClientGetDataResponse> UpdateProfileAsync(string userId, ClientUpdateProfileDTO model)
        {
            var client = await _userManager.Users.OfType<Client>()
         .Include(c => c.City)
             .ThenInclude(city => city.Governorate)
         .FirstOrDefaultAsync(c => c.Id == userId);

            if (client == null)
                throw new InvalidOperationException("Client not found");

            client.UserNameId = model.UserNameId;
            client.PhoneNumber = model.PhoneNumber;
            client.CityId = model.CityId;
            client.Location = model.Location;

            if (model.PersonalPhoto != null)
            {
                client.PersonalPhoto = await _userRepository.SaveImage(model.PersonalPhoto);
            }

            var result = await _userManager.UpdateAsync(client);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _context.SaveChangesAsync();

            // Reload the client to get updated data with relationships
            var updatedClient = await _userManager.Users.OfType<Client>()
                .Include(c => c.City)
                    .ThenInclude(city => city.Governorate)
                .FirstOrDefaultAsync(c => c.Id == userId);

            return new ClientGetDataResponse
            {
                UserNameId = updatedClient.UserNameId,
                PhoneNumber = updatedClient.PhoneNumber,
                PersonalPhoto = updatedClient.PersonalPhoto,
                GovernmentId = updatedClient.City.GovernorateId,
                Government = updatedClient.City.Governorate.Name,
                CityId = updatedClient.CityId,
                City = updatedClient.City.Name,
                Location = updatedClient.Location
            };
        }

        public async Task<GetSelectedProvider> GetSelctedProviderProfile(string ServiceProviderId)
        {
            var serviceProvider = await _context.ServiceProviders.Include(sp => sp.City).ThenInclude(c=>c.Governorate).FirstOrDefaultAsync(sp => sp.Id == ServiceProviderId);

            if (serviceProvider == null)
                throw new InvalidOperationException("Service provider not found");

            return new GetSelectedProvider
            {
                AvailableDays = await _serviceProviderRepository.GetAvailableDays(ServiceProviderId),
                UserNameId = serviceProvider.UserNameId,
                PhoneNumber = serviceProvider.PhoneNumber,
                PersonalPhoto = serviceProvider.PersonalPhoto,
                GovernmentId = serviceProvider.City.GovernorateId,
                Government = serviceProvider.City.Governorate.Name,
                CityId = serviceProvider.CityId,
                City = serviceProvider.City.Name,
                HourPrice = serviceProvider.HourPrice,
                Rate = serviceProvider.Rate
            };
        }

        public async Task<List<AvailabilityResponse>> GetSelectedProviderAvailableDaysOfWeek(string userId)
        {
            try
            {
                var currentDateTime = DateTime.Now;
                var availabilities = new List<AvailabilityResponse>();

                // الحصول على جميع المواعيد المتاحة للبروفايدر
                var allAvailabilities = await _context.Availabilities
                    .Where(a => a.ServiceProciderID == userId)
                    .ToListAsync();

                // تجميع المواعيد حسب التاريخ
                var availabilitiesByDate = allAvailabilities.GroupBy(a => a.Date);

                foreach (var dateGroup in availabilitiesByDate)
                {
                    var date = dateGroup.Key;
                    var availableHours = new List<TimeSpan>();

                    // فحص كل ساعة في هذا التاريخ
                    foreach (var availability in dateGroup)
                    {
                        var requestedDateTime = date.Add(availability.StartTime);

                        // التحقق من أن الوقت في المستقبل
                        if (requestedDateTime > currentDateTime)
                        {
                            // التحقق من عدم وجود حجز مؤكد
                            var isBooked = await _context.Bookings
                                .AnyAsync(b => b.ServiceProviderID == userId &&
                                              b.Day == date &&
                                              b.Houre == availability.StartTime &&
                                              (b.Status == BookingStatus.provider_confirmed ||
                                               b.Status == BookingStatus.paid));

                            // إذا لم يكن محجوز، أضف الساعة
                            if (!isBooked)
                            {
                                availableHours.Add(availability.StartTime);
                            }
                        }
                    }

                    // إضافة اليوم فقط إذا كان يحتوي على ساعات متاحة
                    if (availableHours.Any())
                    {
                        availabilities.Add(new AvailabilityResponse
                        {
                            Date = date,
                            Hours = availableHours.OrderBy(h => h).ToList()
                        });
                    }
                }

                // ترتيب النتائج حسب التاريخ
                return availabilities.OrderBy(a => a.Date).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while getting available days: {ex.Message}");
            }
        }

        public async Task<BookingResponse> CreateBookingAsync(BookingDTO request, string clientId, string ServiceProviderId)
        {
            try
            {
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
                var serviceProvider = await _context.ServiceProviders.FirstOrDefaultAsync(s => s.Id == ServiceProviderId);
                if (serviceProvider == null || client == null)
                    throw new InvalidOperationException("Service provider or client not found.");

                // التحقق من توفر جميع المواعيد المطلوبة
                var unavailableHours = new List<TimeSpan>();
                foreach (var hour in request.Hours)
                {
                    var isSlotAvailable = await IsTimeSlotAvailableAsync(ServiceProviderId, request.Day, hour);
                    if (!isSlotAvailable)
                    {
                        unavailableHours.Add(hour);
                    }
                }

                if (unavailableHours.Any())
                {
                    var unavailableHoursString = string.Join(", ", unavailableHours.Select(h => h.ToString(@"hh\:mm")));
                    throw new InvalidOperationException($"The following time slots are not available: {unavailableHoursString}");
                }

                // إنشاء حجز لكل ساعة
                var bookings = new List<Booking>();
                var bookingIds = new List<int>();

                foreach (var hour in request.Hours)
                {
                    var booking = new Booking
                    {
                        ClientId = clientId,
                        ServiceProviderID = ServiceProviderId,
                        Day = request.Day.Date,
                        Houre = hour,
                        TotalPrice = serviceProvider.HourPrice,
                        Status = BookingStatus.pending,
                        Location = client.Location,
                    };

                    bookings.Add(booking);
                }

                // إضافة جميع الحجوزات
                _context.Bookings.AddRange(bookings);
                await _context.SaveChangesAsync();

                // الحصول على IDs الحجوزات بعد الحفظ
                bookingIds = bookings.Select(b => b.BookingID).ToList();

                // إرسال إشعار واحد يحتوي على جميع الساعات
                await SendBookingNotificationToProviderAsync(serviceProvider.Email, bookings, request.Day);

                return new BookingResponse
                {
                    BookingIDs = bookingIds,
                    ClientId = clientId,
                    ServiceProviderID = ServiceProviderId,
                    Day = request.Day.Date,
                    Hours = request.Hours.OrderBy(h => h).ToList(),
                    Location = client.Location,
                    Status = BookingStatus.pending,
                    TotalPrice = serviceProvider.HourPrice * request.Hours.Count
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while submitting book: {ex.Message}");
            }
        }

        private async Task SendBookingNotificationToProviderAsync(string providerEmail, List<Booking> bookings, DateTime day)
        {
            try
            {
                var hoursString = string.Join(", ", bookings.Select(b => b.Houre.ToString(@"hh\:mm")).OrderBy(h => h));
                var totalHours = bookings.Count;
                var totalPrice = bookings.Sum(b => b.TotalPrice);

                var mailMessage = new Message(
                    new string[] { providerEmail },
                    "New Booking Request",
                     $@"
                <h2 style='font-family: Arial, sans-serif;'>New Booking Received</h2>
                <p style='font-family: Arial, sans-serif;'>You have a new booking with the following details:</p>
                <ul style='font-family: Arial, sans-serif;'>
                    <li><strong>Date:</strong> {day.ToString("dddd, dd/MM/yyyy")}</li>
                    <li><strong>Hours:</strong> {hoursString}</li>
                    <li><strong>Total Hours:</strong> {totalHours} hour(s)</li>
                    <li><strong>Total Price:</strong> {totalPrice:C}</li>
                    <li><strong>Location:</strong> {bookings.First().Location}</li>
                </ul>
                <p style='font-family: Arial, sans-serif;'>Please open your account for more details.</p>"
                );
                await _emailSender.SendEmailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log email sending error
                throw new InvalidOperationException($"{ex.Message}");
            }
        }

        private async Task<bool> IsTimeSlotAvailableAsync(string providerId, DateTime day, TimeSpan hour)
        {
            var workingHours = _context.Availabilities.FirstOrDefault(wh => wh.ServiceProciderID==providerId&& wh.Date == day&&wh.StartTime==hour);
            var requestedDateTime = day.Date.Add(hour);
            if (workingHours == null || requestedDateTime <= DateTime.Now)
            {
                return false;
            }

            // 2. التحقق من عدم وجود حجز آخر في نفس الموعد
            var existingBooking = await _context.Bookings
                .Where(b => b.ServiceProviderID == providerId &&
                           b.Day == day.Date &&
                           b.Houre == hour &&
                           (
                            b.Status == BookingStatus.provider_confirmed ||
                            b.Status == BookingStatus.paid))
                .FirstOrDefaultAsync();

            return existingBooking == null;
        }

        public async Task<ReviewSubmissionResponse> SubmitReviewAsync(ReviewDTO reviewDTO, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Validate the booking exists and belongs to this client
                var booking = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.ServiceProvider)
                    .FirstOrDefaultAsync(b => b.BookingID == reviewDTO.BookingID && b.ClientId==userId);

                if (booking == null)
                {
                    throw new InvalidOperationException("Book not found.");
                }

                // 2. Check if booking is completed (only completed bookings can be reviewed)
                if (booking.Status != BookingStatus.completed)
                {
                    throw new InvalidOperationException("Only completed bookings can be reviewed");
                }

                // 3. Check if review already exists
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.BookingID == reviewDTO.BookingID);

                if (existingReview != null)
                {
                    throw new InvalidOperationException("You've already submitted a review for this booking");
                }

                // 4. Create and save the review
                var review = new Review
                {
                    Rating = reviewDTO.Rating,
                    ReviewDate = DateTime.UtcNow,
                    BookingID = reviewDTO.BookingID
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // 5. Update service provider's average rating (optional)
                await UpdateProviderRating(booking.ServiceProviderID);

                await transaction.CommitAsync();

                return new ReviewSubmissionResponse
                {
                    BookingID=review.BookingID,
                    ClientId=booking.ClientId,
                    Rating=review.Rating,
                    ReviewDate=review.ReviewDate,
                    ServiceProviderId=booking.ServiceProviderID
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error submitting review for booking {BookingId}", reviewDTO.BookingID);
                throw;
            }
        }

        private async Task UpdateProviderRating(string providerId)
        {
            var averageRating = await _context.Reviews
                .Where(r => r.Booking.ServiceProviderID == providerId)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            var provider = await _context.ServiceProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider != null)
            {
                provider.Rate = averageRating;
                await _context.SaveChangesAsync();
            }
        }
    }
}

    
