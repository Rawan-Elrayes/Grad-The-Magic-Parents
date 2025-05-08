using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.Responses
{
    public class GetDataResponse
    {
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string PersonalPhoto { get; set; }
        public string City { get; set; }
        public int? CityId { get; set; }
        public string Government { get; set; }
        public int? GovernmentId { get; set; }
    }

    public class ClientGetDataResponse : GetDataResponse
    {
        public string Location { get; set; }

    }

    public class ProviderGetDataResponse : GetDataResponse
    {
        public double HourPrice { get; set; }

    }


}
