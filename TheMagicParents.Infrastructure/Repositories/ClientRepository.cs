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


        public ClientRepository(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUserRepository userRepository, IServiceProviderRepository serviceProviderRepository, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _serviceProviderRepository = serviceProviderRepository;
            _emailSender = emailSender;
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

            var (token, expires) = await _userRepository.GenerateJwtToken(client);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new ClientRegisterResponse
            {
                Id=client.Id,
                City = _context.Cities.Find(client.CityId).Name,
                Email = client.Email,
                Expires = expires,
                IdCardBackPhoto = client.IdCardBackPhoto,
                IdCardFrontPhoto = client.IdCardFrontPhoto,
                Location = client.Location,
                PersonalPhoto = client.PersonalPhoto,
                PhoneNumber = client.PhoneNumber,
                Token = jwtToken,
                UserNameId = client.UserNameId
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
                var days = await _serviceProviderRepository.GetAvailableDays(userId);

                // إنشاء قائمة للنتيجة النهائية
                var availabilities = new List<AvailabilityResponse>();

                // لكل يوم، الحصول على الساعات المتاحة
                foreach (var day in days)
                {
                    var availability = await _serviceProviderRepository.GetAvailabilitiesHoures(day, userId);
                    availabilities.Add(availability);
                }

                return availabilities;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while getting available days: {ex.Message}");
            }
        }

        public async Task<BookResponse> CreateBookingAsync(BookingDTO request, string clientId, string ServiceProviderId)
        {
            try
            {
                var client = await _context.Clients.FirstOrDefaultAsync(c=>c.Id==clientId);
                var serviceProvider = await _context.ServiceProviders.FirstOrDefaultAsync(s=>s.Id==ServiceProviderId);

                if (serviceProvider == null || client==null)
                    throw new InvalidOperationException("Service provider or client not found.");

                var booking = new Booking
                {
                    ClientId = clientId,
                    ServiceProviderID = ServiceProviderId,
                    Day = request.Day.Date,
                    Houre = request.Houre,
                    TotalPrice = serviceProvider.HourPrice,
                    Status = BookingStatus.paid,
                    Location = client.Location,
                };

                await SendBookingNotificationToProviderAsync(serviceProvider.Email, booking);

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return new BookResponse
                {
                    BookingID = booking.BookingID,
                    ClientId = clientId,
                    ServiceProviderID = ServiceProviderId,
                    Day=booking.Day,
                    Houre = booking.Houre,
                    Location=booking.Location,
                    Status = booking.Status,
                    TotalPrice=booking.TotalPrice
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while submitting book: {ex.Message}");
            }
        }

        private async Task SendBookingNotificationToProviderAsync(string providerEmail, Booking booking)
        {
            try
            {
                var mailMessage = new Message(
                    new string[] { providerEmail },
                    "New Booking Requist",
                     $@"
                        <h2 style='font-family: Arial, sans-serif;'>New Booking Received</h2>
                        <p style='font-family: Arial, sans-serif;'>You have a new booking with the following details:</p>
                        <ul style='font-family: Arial, sans-serif;'>
                            <li><strong>Date:</strong> {booking.Day.ToString("dddd, dd/MM/yyyy")}</li>
                               <li><strong>Time:</strong> {booking.Houre.ToString(@"hh\:mm")}</li>
                            <li><strong>Location:</strong> {booking.Location}</li>
                        </ul>
                        <p style='font-family: Arial, sans-serif;'>Please open your account to more details.</p>"
                );
                await _emailSender.SendEmailAsync(mailMessage);

            }
            catch (Exception ex)
            {
                // Log email sending error
                throw new InvalidOperationException($"{ex.Message}");
            }
        }
    }
}

    
