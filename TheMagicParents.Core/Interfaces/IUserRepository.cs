using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Models;
using System.IdentityModel.Tokens.Jwt;
using TheMagicParents.Core.Responses;
using TheMagicParents.Enums;
using TheMagicParents.Core.DTOs;

namespace TheMagicParents.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<Governorate>> GetGovernorateAsync();
        Task<IEnumerable<City>> GetCitiesByGovernorateAsync(int GovernorateId);
        Task<string> GenerateUserNameIdFromEmailAsync(string email);
        Task<string> SaveImage(IFormFile image);
        Task<(JwtSecurityToken Token, DateTime Expires)> GenerateJwtToken<TUser>(TUser user) where TUser : User;

        Task<bool> SubmitReportAsync(string reporterUserId, string reportedUserNameId, string comment);
        Task<IEnumerable<Support>> GetPendingReportsAsync();
        Task<bool> HandleReportAsync(int reportId, bool isImportant);
        Task<List<BookingStatusRsponse>> GetPendingBookingsAsync(string userId);
        Task<List<BookingStatusRsponse>> GetProviderConfirmedBookingsAsync(string userId);
        Task<List<BookingStatusRsponse>> GetPaidBookingsAsync(string userId);
        Task<List<BookingStatusRsponse>> GetCancelledBookingsAsync(string userId);
        Task<List<BookingStatusRsponse>> GetCompletedBookingsAsync(string userId);
        Task<List<BookingStatusRsponse>> GetRejectedBookingsAsync(string userId);
        Task<CancelBookingResponse> CancelBookingAsync(int bookingId, string userId);
    }
}
