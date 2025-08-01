﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.Responses;
using TheMagicParents.Enums;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Interfaces
{
    public interface IClientRepository
    {
        Task<ClientRegisterResponse> RegisterClientAsync(ClientRegisterDTO model);
        Task<ClientGetDataResponse> GetProfileAsync(string userId);
        Task<ClientGetDataResponse> UpdateProfileAsync(string userId, ClientUpdateProfileDTO model);
        Task<GetSelectedProvider> GetSelctedProviderProfile (string ServiceProviderId);
        Task<List<AvailabilityResponse>> GetSelectedProviderAvailableDaysOfWeek(string userId);
        Task<BookingResponse> CreateBookingAsync(BookingDTO bookingDTO, string clientId, string ServiceProviderId);
        Task<ReviewSubmissionResponse> SubmitReviewAsync(ReviewDTO review, string userId);
    }
}
