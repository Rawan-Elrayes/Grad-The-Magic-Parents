﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Interfaces
{
    interface IServiceProviderRepository
    {
        Task<Client> RegisterServiceProviderAsync(ServiceProviderRegisterDTO model);
    }
}
