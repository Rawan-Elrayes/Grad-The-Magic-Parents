using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Responses
{
    public class ServiceProviderRegisterResponse:UserRegisterResponse
    {
        
        public ServiceType Type { get; set; }
        public string? Certification { get; set; }
        public double HourPrice { get; set; }
    }
}
