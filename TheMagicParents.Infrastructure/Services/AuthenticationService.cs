using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.EmailService;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Models;

namespace TheMagicParents.Infrastructure.Services
{
    public class AuthenticationService : IAuthentication
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AuthenticationService(
            UserManager<User> userManager, 
            SignInManager<User> signInManager, 
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task<Response<string>> LoginAsync(LoginDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return new Response<string> { Status = false, Message = "Invalid email or password" };

            // Check if email is confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
                return new Response<string> { Status = false, Message = "Please confirm your email before logging in" };

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, true);
            if (!result.Succeeded)
                return new Response<string> { Status = false, Message = "Invalid email or password" };

            return new Response<string> { Status = true, Message = "Login successful", Data = user.Id };
        }

        public async Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return new Response<string> { Status = false, Message = "Email not found" };

            // Generate token and encode it
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            
            // Create reset link with encoded token
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7777";
            var resetLink = $"{baseUrl}/reset-password?email={WebUtility.UrlEncode(model.Email)}&token={encodedToken}";

            var message = new Message(
                new string[] { model.Email },
                "Reset Password",
                $@"<h4>Reset Password</h4>
                   <p>Please click the link below to reset your password. This link will expire in 24 hours.</p>
                   <p><a href='{resetLink}'>Reset Password</a></p>
                   <p>If you didn't request this, please ignore this email.</p>"
            );

            await _emailSender.SendEmailAsync(message);

            return new Response<string> { Status = true, Message = "Password reset link has been sent to your email" };
        }

        public async Task<Response<string>> ResetPasswordAsync(ResetPasswordDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return new Response<string> { Status = false, Message = "User not found" };

            // Decode token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
            if (!result.Succeeded)
                return new Response<string> { Status = false, Message = "Failed to reset password" };

            return new Response<string> { Status = true, Message = "Password has been reset successfully" };
        }

        public async Task<Response<string>> LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return new Response<string> { Status = true, Message = "Logged out successfully" };
        }

        public async Task<Response<string>> SendEmailConfirmationAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new Response<string> { Status = false, Message = "User not found" };

            if (await _userManager.IsEmailConfirmedAsync(user))
                return new Response<string> { Status = false, Message = "Email is already confirmed" };

            // Generate token and encode it
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Create confirmation link with encoded token
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7777";
            var confirmationLink = $"{baseUrl}/api/account/confirm-email?userId={user.Id}&token={encodedToken}";

            var message = new Message(
                new string[] { email },
                "Confirm your email",
                $@"<h4>Welcome to The Magic Parents!</h4>
                   <p>Please confirm your email address by clicking the link below. This link will expire in 24 hours.</p>
                   <p><a href='{confirmationLink}'>Confirm Email</a></p>
                   <p>If you didn't create an account with us, please ignore this email.</p>"
            );

            await _emailSender.SendEmailAsync(message);

            return new Response<string> { Status = true, Message = "Confirmation email sent successfully" };
        }

        public async Task<Response<string>> ConfirmEmailAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new Response<string> { Status = false, Message = "User not found" };

            // Decode token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                return new Response<string> { Status = false, Message = "Failed to confirm email" };

            return new Response<string> { Status = true, Message = "Email confirmed successfully" };
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;
                
            return await _userManager.FindByIdAsync(userId);
        }
    }
}
