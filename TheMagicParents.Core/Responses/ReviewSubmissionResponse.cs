using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.Responses
{
    public class ReviewSubmissionResponse
    {
        public int Rating { get; set; }
        public DateTime ReviewDate { get; set; }
        public int BookingID { get; set; }
        public string ServiceProviderId { get; set; }
        public string ClientId { get; set; }
    }
}
