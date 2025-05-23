﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;
using TheMagicParents.Models;

namespace TheMagicParents.Core.Responses
{
    public class BookResponse
    {
        public int BookingID { get; set; }
        public string ClientId { get; set; }
        public string ServiceProviderID { get; set; }
        public DateTime Day { get; set; }
        public TimeSpan Houre { get; set; }
        public double TotalPrice { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.pending;
        public string Location { get; set; }
    }
}
