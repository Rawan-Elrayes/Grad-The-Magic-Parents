using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Responses
{
    public class AvailabilityResponse
    {
        public DateTime Date { get; set; }
        public List<TimeSpan> Houres { get; set; }
    }
}
