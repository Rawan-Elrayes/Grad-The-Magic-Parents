using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.Responses
{
    public class UserRegisterResponse
    {
        public string Id { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public string UserNameId { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string PersonalPhoto { get; set; }
        public string IdCardFrontPhoto { get; set; }
        public string IdCardBackPhoto { get; set; }
        public string City { get; set; }
        public string Government { get; set; }

    }
}
