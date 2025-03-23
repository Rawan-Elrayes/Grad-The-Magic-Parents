using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.DTOs
{

    public class UserRegisterDTO
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [MinLength(11), MaxLength(11)]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public IFormFile PersonalPhoto { get; set; }

        [Required]
        public IFormFile IdCardFrontPhoto { get; set; }
        
        [Required]
        public IFormFile IdCardBackPhoto { get; set; }

        [Required]
        [DisplayName("City")]
        public int CityId { get; set; }
    }
}
