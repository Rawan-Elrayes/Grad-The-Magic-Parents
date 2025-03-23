using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.Sharing
{
    public class EmailStringBody
    {
        public static string send (string email , string token , string component , string title)
        {
            //component : active account or reset password

            var EncodeToken = Uri.EscapeDataString(token);
            return $@"




                    ";
        }
    }
}
