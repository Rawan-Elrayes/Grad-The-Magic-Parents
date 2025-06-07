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
using TheMagicParents.Core.Responses;

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
                Status = 1,
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
                UserNameId = model.UserNameId,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                AccountState = StateType.Active,
                PasswordHash = model.Password,
                EmailConfirmed = true
            };

            Admin.UserName = await _userRepository.GenerateUserNameIdFromEmailAsync(Admin.Email);

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

            var res = new AdminRegisterResponse
            {
                Email = Admin.Email,
                PhoneNumber = Admin.PhoneNumber,
                UserNameId = Admin.UserNameId,
                Password = Admin.PasswordHash
            };

            return Ok(new Response<AdminRegisterResponse>
            {
                Message = "Admin registered successfully",
                Data = res,
                Status = 0
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new Response<AdminRegisterResponse>
            {
                Message = ex.Message,
                Status = 1,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new Response<AdminRegisterResponse>
            {
                Message = "An error occurred while registering the admin.",
                Status = 1,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpGet("pending-users")]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
        }
    }

    [HttpGet("pending-deletions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingDeletions()
    {
        var pendingDeletions = await _userManager.Users
            .Where(u => u.AccountState == StateType.PendingDeletion)
            .Select(u => new
            {
                u.Id,
                u.UserNameId,
                UserType = _userManager.GetRolesAsync(u).Result.FirstOrDefault(),
                ActiveBookings = _context.Bookings
                    .Where(b => (b.ClientId == u.Id || b.ServiceProviderID == u.Id) &&
                               (b.Status == BookingStatus.pending || b.Status == BookingStatus.provider_confirmed || b.Status == BookingStatus.paid))
                    .Select(b => new
                    {
                        b.BookingID,
                        b.Status,
                        b.Day
                    }).ToList(),
                HasActiveBookings = _context.Bookings
                .Any(b => (b.ClientId == u.Id || b.ServiceProviderID == u.Id) &&
                               (b.Status == BookingStatus.pending || b.Status == BookingStatus.provider_confirmed || b.Status == BookingStatus.paid))
            })
            .ToListAsync();

        return Ok(pendingDeletions);
    }

    [HttpPost("handle-deletion/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HandleDeletionRequest(string userId, [FromBody] bool isApproved)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        if (isApproved)
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest("Failed to delete user");

            return Ok("User deleted successfully");
        }
        else
        {
            user.AccountState = StateType.Active;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest("Failed to reject deletion request");

            return Ok("Deletion request rejected");
        }
    }

    [HttpGet("pending-reports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingReports()
    {
        var reports = await _userRepository.GetPendingReportsAsync();

        var reportData = reports.Select(r => new
        {
            ReportId = r.SupportID,
            ReportedUser = r.user.UserNameId,
            ReportedUserNameId = r.user.UserName,
            ComplainerName = _userManager.FindByIdAsync(r.ComplainerId.ToString()).Result?.UserNameId,
            Comment = r.Comment,
            CurrentSupportCount = r.user.NumberOfSupports
        });

        return Ok(new Response<object>
        {
            Data = reportData,
            Status = 0,
            Message = "Pending reports retrieved successfully"
        });
    }

    [HttpPost("handle-report/{reportId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HandleReport(int reportId, [FromBody] bool isImportant)
    {
        var result = await _userRepository.HandleReportAsync(reportId, isImportant);

        if (!result)
            return NotFound(new Response<string>
            {
                Message = "Report not found",
                Status = 1
            });

        return Ok(new Response<string>
        {
            Message = $"Report {(isImportant ? "approved" : "rejected")} successfully",
            Status = 0
        });
    }
}