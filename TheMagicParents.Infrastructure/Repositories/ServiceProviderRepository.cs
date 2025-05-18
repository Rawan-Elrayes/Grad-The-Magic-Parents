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
//using System.Linq.Dynamic.Core;


namespace TheMagicParents.Infrastructure.Repositories
{
    public class ServiceProviderRepository : IServiceProviderRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _userRepository;


        public ServiceProviderRepository(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUserRepository userRepository)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
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

            var (token, expires) = await _userRepository.GenerateJwtToken(ServiceProvider);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new ServiceProviderRegisterResponse
            {
                Id = ServiceProvider.Id,
                City = _context.Cities.Find(ServiceProvider.CityId).Name,
                Email = ServiceProvider.Email,
                Expires = expires,
                IdCardBackPhoto = ServiceProvider.IdCardBackPhoto,
                IdCardFrontPhoto = ServiceProvider.IdCardFrontPhoto,
                Certification = ServiceProvider.Certification,
                PersonalPhoto = ServiceProvider.PersonalPhoto,
                PhoneNumber = ServiceProvider.PhoneNumber,
                Token = jwtToken,
                UserNameId = ServiceProvider.UserNameId
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
                // Create new availabilities
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

                // Add new ones
                await _context.Availabilities.AddRangeAsync(newAvailabilities);
                await _context.SaveChangesAsync();

                return new AvailabilityResponse
                {
                    Date=request.Date.Date,
                    Houres=newAvailabilities.Select(a=>a.StartTime).ToList()
                };

            }
            catch (Exception ex)
            {
                // Log error here
                throw new InvalidOperationException("An error occurred while saving availability");
                
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
                    Houres = availabilities
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
                .Where(a => a.ServiceProciderID == userId).Select(a => a.Date.Date)
                .ToListAsync();

                return availabilities;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while getting available days.");
            }
        }

    }
}