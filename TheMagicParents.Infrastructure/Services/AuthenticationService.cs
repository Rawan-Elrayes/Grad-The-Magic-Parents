using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.EmailService;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Core.Responses;
using TheMagicParents.Enums;
using TheMagicParents.Infrastructure.Repositories;
using TheMagicParents.Models;

namespace TheMagicParents.Infrastructure.Services
{
    public class AuthenticationService : IAuthentication
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;

        public AuthenticationService(
            UserManager<User> userManager, 
            SignInManager<User> signInManager, 
            IEmailSender emailSender,
            IConfiguration configuration,
            IUserRepository userRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _configuration = configuration;
            _userRepository = userRepository;
        }

        public async Task<Response<LoginResponse>> LoginAsync(LoginDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return new Response<LoginResponse> { Status = 1, Message = "Invalid email or password" };

            

            // Check if email is confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return new Response<LoginResponse> { Status = 1, Message = "Please confirm your email before logging in" };
            }
            user.AccountState = Enums.StateType.Active;

            
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, true);
            if (!result.Succeeded)
                return new Response<LoginResponse> { Status = 1, Message = "Invalid email or password" };

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expires) = await _userRepository.GenerateJwtToken(user);
            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new Response<LoginResponse> { Status = 0, Message = "Login successful", Data = new LoginResponse { userId = user.Id, Token = jwtToken, TokenExpire = expires, UserName=user.UserName, UserType=roles.ToList() } };
        }

        public async Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return new Response<string> { Status = 1, Message = "Email not found" };

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

            return new Response<string> { Status = 0, Message = "Password reset link has been sent to your email" , Token = encodedToken };
        }

        public async Task<Response<string>> ResetPasswordAsync(ResetPasswordDTO model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return new Response<string> { Status = 1, Message = "User not found" };

            // Decode token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
            if (!result.Succeeded)
                return new Response<string> { Status = 1, Message = "Failed to reset password" };

            return new Response<string> { Status = 0, Message = "Password has been reset successfully" };
        }

        public async Task<Response<string>> LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return new Response<string> { Status = 0, Message = "Logged out successfully" };
        }

        public async Task<Response<string>> SendEmailConfirmationAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new Response<string> { Status = 1, Message = "User not found" };

            if (await _userManager.IsEmailConfirmedAsync(user))
                return new Response<string> { Status = 1, Message = "Email is already confirmed" };

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

            return new Response<string> { Status = 0, Message = "Confirmation email sent successfully" };
        }

        public async Task<Response<string>> ConfirmEmailAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new Response<string> { Status = 1, Message = "User not found" };

            // Decode token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                return new Response<string> { Status = 1, Message = "Failed to confirm email" };

            return new Response<string> { Status = 0, Message = "Email confirmed successfully" };
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;
                
            return await _userManager.FindByIdAsync(userId);
        }
    }
}
