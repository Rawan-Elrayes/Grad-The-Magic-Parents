using TheMagicParents.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Microsoft.EntityFrameworkCore;
using TheMagicParents.Models;
using TheMagicParents.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using TheMagicParents.Core.EmailService;
using System.IdentityModel.Tokens.Jwt;
using TheMagicParents.Infrastructure.Data;

//[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;
    private readonly IUserRepository _userRepository;

    public AdminController(UserManager<User> userManager, IEmailSender emailSender, IHttpContextAccessor httpContextAccessor, RoleManager<IdentityRole> roleManager, AppDbContext context, IUserRepository userRepository)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
        _roleManager = roleManager;
        _context = context;
        _userRepository = userRepository;
    }

    [HttpPost("register/admin")]
    public async Task<IActionResult> RegisterAdmin([FromForm] AdminRegisterDTO model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new Response<AdminRegisterResponse>
            {
                Message = "Invalid model state",
                Status = false,
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage).ToList()
            });
        }

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var Admin = new User
            {
                UserName = model.UserName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                AccountState = StateType.Active, 
                PasswordHash = model.Password,
                EmailConfirmed = true
            };

            // ÅäÔÇÁ ÇáãÓÊÎÏã
            var result = await _userManager.CreateAsync(Admin, model.Password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            if (!await _roleManager.RoleExistsAsync(UserRoles.Admin.ToString()))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin.ToString()));
            }
            await _userManager.AddToRoleAsync(Admin, UserRoles.Admin.ToString());

            await _context.SaveChangesAsync();

            //var (token, expires) = await _userRepository.GenerateJwtToken(Admin);
            //var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            var res = new AdminRegisterResponse
            {
                Email = Admin.Email,
                PhoneNumber = Admin.PhoneNumber,
                UserName = Admin.UserName,
                Password = Admin.PasswordHash
                
            };

            return Ok(new Response<AdminRegisterResponse>
            {
                Message = "Admin registered successfully",
                Data = res,
                Status = true
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new Response<AdminRegisterResponse>
            {
                Message = ex.Message,
                Status = false,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new Response<AdminRegisterResponse>
            {
                Message = "An error occurred while registering the admin.",
                Status = false,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("pending-users")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var pendingUsers = await _userManager.Users
            .Where(u => !u.EmailConfirmed && u.AccountState == StateType.Waiting)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.UserNameId,
                u.IdCardFrontPhoto,
                u.PersonWithCard
            })
            .ToListAsync();

        return Ok(pendingUsers);
    }

    [HttpPost("verify-user/{userId}")]
    public async Task<IActionResult> VerifyUser(string userId, [FromBody] bool isApproved)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        if (isApproved)
        {
            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailConfirmationToken));

            var callbackUrl = $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}/api/email/confirm-email?userId={user.Id}&token={encodedToken}";

            var message = new Message(new string[] { user.Email! }, "Welcome To The Magic Parents",
                $"<h3>Welcome {user.UserNameId}!</h3>" +
                "<p>Your identity has been verified. Please confirm your email:</p>" +
                $"<p><a href='{callbackUrl}'>Confirm</a></p>" +
                "<p>You have only 24 hours to confirm.</p>");

            _emailSender.SendEmail(message);
            return Ok("User verified and confirmation email sent");
        }
        else
        {
            await _userManager.DeleteAsync(user);
            return Ok("User rejected and account deleted");

            //user.AccountState = StateType.Blocked;
            //await _userManager.UpdateAsync(user);
            //return Ok("User verification rejected");
        }
    }
}