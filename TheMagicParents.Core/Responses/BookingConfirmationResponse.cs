using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;

namespace TheMagicParents.Core.Responses
{
    public class BookingConfirmationResponse
    {
        public int BookingId { get; set; }
        public BookingStatus NewStatus { get; set; }
    }
}
