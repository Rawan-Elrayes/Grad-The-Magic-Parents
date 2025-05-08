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

            public async Task<bool> SubmitReportAsync(string reporterUserId, string reportedUserNameId, string comment)
            {
                var reportedUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.UserNameId == reportedUserNameId);

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
        

    }
}
