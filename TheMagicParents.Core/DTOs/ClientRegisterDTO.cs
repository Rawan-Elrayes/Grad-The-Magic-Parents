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
    public class ClientRegisterDTO:UserRegisterDTO
    {
        [Required]
        [DisplayName("Current address")]
        public string Location { get; set; }
    }
}
