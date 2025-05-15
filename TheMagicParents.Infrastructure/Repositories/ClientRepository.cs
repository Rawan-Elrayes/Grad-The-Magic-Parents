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

namespace TheMagicParents.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _userRepository;


        public ClientRepository(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IUserRepository userRepository)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
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

            // التحقق من وجود دور Customer وإنشائه إذا لم يكن موجودًا
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
    }
}