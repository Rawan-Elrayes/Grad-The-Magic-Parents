using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.Responses;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Interfaces
{
    public interface IServiceProviderRepository
    {
        Task<ServiceProviderRegisterResponse> RegisterServiceProviderAsync(ServiceProviderRegisterDTO model);
        Task<AvailabilityResponse> SaveAvailability(AvailabilityDTO request, string Id);
        Task<AvailabilityResponse> GetAvailabilitiesHoures(DateTime date, string Id);
        Task<ProviderGetDataResponse> GetProfileAsync(string userId);
        Task<ProviderGetDataResponse> UpdateProfileAsync(string userId, ServiceProviderUpdateProfileDTO model);
        Task<List<DateTime>> GetAvailableDays(string userId);

        //search for providers 
        Task<PagedResult<FilteredProviderDTO>> GetFilteredProvidersAsync(ProviderFilterDTO filter, int page = 1, int pageSize = 8 );
    }
}
