using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Models;

namespace TheMagicParents.Core.DTOs
{
    public class ReviewDTO
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        public int BookingID { get; set; }
    }
}
