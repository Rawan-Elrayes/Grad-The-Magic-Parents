using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.DTOs
{
    public class UpdateProfileDTO
    {
        
            public string UserNameId { get; set; }
            public string PhoneNumber { get; set; }
            public int GovernmentId { get; set; }
            public int CityId { get; set; }
            public IFormFile PersonalPhoto { get; set; }
    }

        public class ClientUpdateProfileDTO : UpdateProfileDTO
        {
            public string Location { get; set; }
        }

        public class ServiceProviderUpdateProfileDTO : UpdateProfileDTO
        {
            public double HourPrice { get; set; }
        }
    
}
