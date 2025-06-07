using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;

namespace TheMagicParents.Core.Responses
{
    public class CancelBookingResponse
    {
        public int? BookingId { get; set; }
        public BookingStatus? NewStatus { get; set; }
        public string? CancelledBy { get; set; }
    }
}
