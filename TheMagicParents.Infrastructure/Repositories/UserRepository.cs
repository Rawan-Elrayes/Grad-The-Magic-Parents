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

namespace TheMagicParents.Infrastructure.Repositories
{
    public class UserRepository:IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        
        public UserRepository(AppDbContext context, UserManager<User> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<IEnumerable<Governorate>> GetGovernmentsAsync()
        {
            return await _context.Governorates.ToListAsync();
        }

        public async Task<IEnumerable<City>> GetCitiesByGovernmentAsync(int governmentId)
        {
            return await _context.Cities
                .Where(c => c.GovernorateId == governmentId)
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
    }
}
