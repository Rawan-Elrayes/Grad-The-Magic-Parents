﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Models;
using System.IdentityModel.Tokens.Jwt;

namespace TheMagicParents.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<Governorate>> GetGovernmentsAsync();
        Task<IEnumerable<City>> GetCitiesByGovernmentAsync(int governmentId);
        Task<string> GenerateUserNameIdFromEmailAsync(string email);
        Task<string> SaveImage(IFormFile image);
        Task<(JwtSecurityToken Token, DateTime Expires)> GenerateJwtToken<TUser>(TUser user) where TUser : User;
    }
}
