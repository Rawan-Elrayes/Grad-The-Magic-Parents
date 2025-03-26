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

namespace TheMagicParents.Infrastructure.Repositories
{
    public class ServiceProviderRepository : IServiceProviderRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public ServiceProviderRepository(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUserRepository userRepository, IEmailSender emailSender, IConfiguration configuration, ILogger<UserRepository> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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
                UserNameId = model.UserName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                PersonalPhoto = await _userRepository.SaveImage(model.PersonalPhoto),
                IdCardFrontPhoto = await _userRepository.SaveImage(model.IdCardFrontPhoto),
                IdCardBackPhoto = await _userRepository.SaveImage(model.IdCardBackPhoto),
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

            // توليد توكن تأكيد البريد
            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(ServiceProvider);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailConfirmationToken));

            try
            {
                var callbackUrl = $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}/api/email/confirm-email?userId={ServiceProvider.Id}&token={encodedToken}";

                var message = new Message(new string[] { ServiceProvider.Email! }, "Welcome To The Magic Parents",
                    $"<h3>Welcome {ServiceProvider.UserNameId}!</h3>" +
                    "<p>Thanks for use our application, Please confirm you E-mail:</p>" +
                    $"<p><a href='{callbackUrl}'>Confirm</a></p>" +
                    "<p>You have only 24 hours to confirm, If you don't register by this email you can ignore it.</p>");

                _emailSender.SendEmail(message);

                var (token, expires) = await _userRepository.GenerateJwtToken(ServiceProvider);
                var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

                return new ServiceProviderRegisterResponse
                {
                    City = _context.Cities.Find(ServiceProvider.CityId).Name,
                    Email = ServiceProvider.Email,
                    Expires = expires,
                    IdCardBackPhoto = ServiceProvider.IdCardBackPhoto,
                    IdCardFrontPhoto = ServiceProvider.IdCardFrontPhoto,
                    Certification = ServiceProvider.Certification,
                    PersonalPhoto = ServiceProvider.PersonalPhoto,
                    PhoneNumber = ServiceProvider.PhoneNumber,
                    Token = jwtToken,
                    UserName = ServiceProvider.UserName
                };
            }
            catch (Exception ex)
            {
                // حذف المستخدم إذا فشل إرسال البريد
                await _userManager.DeleteAsync(ServiceProvider);
                _logger.LogError(ex, "فشل إرسال بريد التأكيد");

                throw new ApplicationException("فشل إرسال بريد التأكيد، يرجى المحاولة لاحقاً");
            }

            //HttpContext.Session.SetString("UserId", client.Id.ToString());
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
                throw new ArgumentException("الملف المرفوع غير صالح أو فارغ");
            }
        
            // التحقق من أن الملف هو PDF
            var extension = Path.GetExtension(pdfFile.FileName).ToLower();
            if (extension != ".pdf")
            {
                throw new ArgumentException("الملف يجب أن يكون بصيغة PDF");
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
    }
}