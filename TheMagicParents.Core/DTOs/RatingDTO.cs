using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.DTOs
{
    public class RatingDTO
    {
        public string ServiceProviderId { get; set; }
        public int Rate { get; set; }
        public int BookingId { get; set; }
    }
}
