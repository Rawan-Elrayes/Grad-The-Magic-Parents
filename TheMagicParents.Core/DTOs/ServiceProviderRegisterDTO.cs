using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;
using TheMagicParents.Models;

namespace TheMagicParents.Core.DTOs
{
    class ServiceProviderRegisterDTO:UserRegisterDTO
    {
        public ServiceType Type { get; set; }
        public string? Certification { get; set; }

        [Required]
        public double HourPrice { get; set; }
    }
}