using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Interfaces
{
    public interface IAuthentication
    {
        Task<Response<string>> LoginAsync(LoginDTO model);
        Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDTO model);
        Task<Response<string>> ResetPasswordAsync(ResetPasswordDTO model);
        Task<Response<string>> LogoutAsync();
        Task<Response<string>> SendEmailConfirmationAsync(string email);
        Task<Response<string>> ConfirmEmailAsync(string email, string token);
        Task<User?> GetUserByIdAsync(string userId);
    }
}
