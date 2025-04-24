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
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _userRepository;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserRepository> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public ClientRepository(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUserRepository userRepository, IEmailSender emailSender, IConfiguration configuration, ILogger<UserRepository> logger, IHttpContextAccessor httpContextAccessor)
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

        public async Task<ClientRegisterResponse> RegisterClientAsync(ClientRegisterDTO model)
        {
            // التحقق من وجود البريد الإلكتروني مسبقًا
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var client = new Client
            {
                UserNameId = model.UserName,
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

            // التحقق من وجود دور Customer وإنشائه إذا لم يكن موجودًا
            if (!await _roleManager.RoleExistsAsync(UserRoles.Client.ToString()))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Client.ToString()));
            }
            await _userManager.AddToRoleAsync(client, UserRoles.Client.ToString());

            await _context.SaveChangesAsync();
            /*
            // توليد توكن تأكيد البريد
            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(client);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailConfirmationToken));

            try
            {
                var callbackUrl = $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}/api/email/confirm-email?userId={client.Id}&token={encodedToken}";

                var message = new Message(new string[] { client.Email! }, "Welcome To The Magic Parents", GetHtmlContent(client.UserNameId, callbackUrl));

                _emailSender.SendEmail(message);

                var (token, expires) = await _userRepository.GenerateJwtToken(client);
                var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

                return new ClientRegisterResponse
                {
                    City=_context.Cities.Find(client.CityId).Name,
                    Email=client.Email,
                    Expires=expires,
                    IdCardBackPhoto=client.IdCardBackPhoto,
                    IdCardFrontPhoto=client.IdCardFrontPhoto,
                    Location=client.Location,
                    PersonalPhoto=client.PersonalPhoto,
                    PhoneNumber=client.PhoneNumber,
                    Token=jwtToken,
                    UserName = client.UserName
                };
            }
            catch (Exception ex)
            {
                // حذف المستخدم إذا فشل إرسال البريد
                await _userManager.DeleteAsync(client);
                _logger.LogError(ex, "فشل إرسال بريد التأكيد");

                throw new ApplicationException("فشل إرسال بريد التأكيد، يرجى المحاولة لاحقاً");
            }

            //HttpContext.Session.SetString("UserId", client.Id.ToString());

           */


            //--- Edit after comment email confirmation
            var (token, expires) = await _userRepository.GenerateJwtToken(client);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new ClientRegisterResponse
            {
                City = _context.Cities.Find(client.CityId).Name,
                Email = client.Email,
                Expires = expires,
                IdCardBackPhoto = client.IdCardBackPhoto,
                IdCardFrontPhoto = client.IdCardFrontPhoto,
                Location = client.Location,
                PersonalPhoto = client.PersonalPhoto,
                PhoneNumber = client.PhoneNumber,
                Token = jwtToken,
                UserName = client.UserName
            };
        }

        private string GetHtmlContent(string UserName, string URL)
        {
            return $"<h3>Welcome {UserName}!</h3>" +
                    "<p>Thanks for use our application, Please confirm you E-mail:</p>" +
                    $"<p><a href='{URL}'>Confirm</a></p>" +
                    "<p>You have only 24 hours to confirm, If you don't register by this email you can ignore it.</p>";
        }
    }
}