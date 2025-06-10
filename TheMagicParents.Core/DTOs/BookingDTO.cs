using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMagicParents.Enums;
using TheMagicParents.Models;

namespace TheMagicParents.Core.DTOs
{
    public class BookingDTO
    {
        [Required]
        public DateTime Day { get; set; }

        [Required]
        public List<TimeSpan> Hours { get; set; }
    }
}
