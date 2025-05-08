using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.Responses;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Interfaces
{
    public interface IClientRepository
    {
        Task<ClientRegisterResponse> RegisterClientAsync(ClientRegisterDTO model);
        Task<ClientGetDataResponse> GetProfileAsync(string userId);
        Task<ClientGetDataResponse> UpdateProfileAsync(string userId, ClientUpdateProfileDTO model);
    }

}
