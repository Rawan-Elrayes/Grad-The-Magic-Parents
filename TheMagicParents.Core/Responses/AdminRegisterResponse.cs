﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.Responses
{
    public class AdminRegisterResponse
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string UserNameId { get; set; }
        public string PhoneNumber { get; set; }
    }
}
