using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;

namespace TheMagicParents.Core.Responses
{
    public class BookingStatusRsponse
    {
        public int BookingID { get; set; }
        public string ClientId { get; set; }
        public string ServiceProviderID { get; set; }
        public DateTime Day { get; set; }
        public TimeSpan Hours { get; set; }
        public double TotalPrice { get; set; }
        public BookingStatus Status { get; set; }
        public string Location { get; set; }
        public string ClientName { get; set; }
        public string ServiceProviderName { get; set; }
        public ServiceType ServiceType { get; set; }
    }
}
