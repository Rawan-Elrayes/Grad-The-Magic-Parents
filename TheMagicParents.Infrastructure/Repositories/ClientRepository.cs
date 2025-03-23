using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Infrastructure.Data;
using TheMagicParents.Models;
using TheMagicParents.Enums;
using TheMagicParents.Core.DTOs;

namespace TheMagicParents.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository userRepository;

        public ClientRepository(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUserRepository userRepository)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            this.userRepository = userRepository;
        }

        public async Task<Client> RegisterClientAsync(ClientRegisterDTO model)
        {
            // التحقق من وجود البريد الإلكتروني مسبقًا
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var client = new Client
            {
                UserName = model.UserName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                PersonalPhoto = await userRepository.SaveImage(model.PersonalPhoto),
                IdCardFrontPhoto = await userRepository.SaveImage(model.IdCardFrontPhoto),
                IdCardBackPhoto = await userRepository.SaveImage(model.IdCardBackPhoto),
                CityId = model.CityId,
                AccountState = StateType.Active,
                Location = model.Location,
                PasswordHash = model.Password
            };

            // توليد UserNameId من البريد الإلكتروني
            client.UserNameId = await userRepository.GenerateUserNameIdFromEmailAsync(client.Email);

            // إنشاء المستخدم
            var result = await _userManager.CreateAsync(client, model.Password);
            if (!result.Succeeded)
            {
                throw new Exception("User creation failed.");
            }

            // التحقق من وجود دور Customer وإنشائه إذا لم يكن موجودًا
            if (!await _roleManager.RoleExistsAsync(UserRoles.Client.ToString()))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Client.ToString()));
            }

            // إضافة المستخدم إلى دور Customer
            if (await _roleManager.RoleExistsAsync(UserRoles.Client.ToString()))
            {
                await _userManager.AddToRoleAsync(client, UserRoles.Client.ToString());
            }

            //HttpContext.Session.SetString("UserId", client.Id.ToString());

            //await _context.Clients.AddAsync(client);
            await _context.SaveChangesAsync();
            return client;
        }

        
    }
}