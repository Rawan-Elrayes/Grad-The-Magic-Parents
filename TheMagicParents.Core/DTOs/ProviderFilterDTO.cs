using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;

namespace TheMagicParents.Core.DTOs
{
    public class ProviderFilterDTO
    {
        public ServiceType ServiceType { get; set; }
        public int GovernmentId { get; set; }
        public int CityId { get; set; }
        public DateTime Date { get; set; }
        public string ClientAddress { get; set; }
    }

    public class FilteredProviderDTO
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string PersonalPhoto { get; set; }
        public double Rating { get; set; }
        public double PricePerHour { get; set; }
        public bool IsAvailableOnDate { get; set; }  // Simply indicates if provider is available on the requested date
    }
}
