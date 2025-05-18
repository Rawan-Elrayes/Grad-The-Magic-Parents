using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using TheMagicParents.Enums;
using TheMagicParents.Infrastructure.Data;
using TheMagicParents.Models;

[Route("api/[controller]")]
[ApiController]
public class EmailController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;

    public EmailController(UserManager<User> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        try
        {
            // فك تشفير التوكن
            var decodedToken = WebEncoders.Base64UrlDecode(token);
            var normalToken = Encoding.UTF8.GetString(decodedToken);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // تأكيد البريد الإلكتروني
            var result = await _userManager.ConfirmEmailAsync(user, normalToken);

            if (result.Succeeded)
            {
                // تغيير حالة المستخدم إلى Active
                if (user is Client client) // إذا كان المستخدم من نوع Client
                {
                    client.AccountState = StateType.Active;
                    await _context.SaveChangesAsync();
                }
                else if (user is TheMagicParents.Models.ServiceProvider provider)
                {
                    provider.AccountState = StateType.Active;
                    await _context.SaveChangesAsync();
                }

                    return Ok(new
                    {
                        Success = true,
                        Message = "Email confirmed successfully and account activated"
                    });
            }

            return BadRequest(new
            {
                Success = false,
                Message = "Email confirmation failed",
                Errors = result.Errors.Select(e => e.Description)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "An error occurred while confirming email",
                Error = ex.Message
            });
        }
    }

}