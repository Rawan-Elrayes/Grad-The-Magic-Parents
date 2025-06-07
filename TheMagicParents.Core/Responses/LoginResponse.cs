using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.Responses
{
    public class LoginResponse
    {
        public string userId { get; set; }
        public string Token { get; set; }
        public DateTime TokenExpire { get; set; }
    }
}
