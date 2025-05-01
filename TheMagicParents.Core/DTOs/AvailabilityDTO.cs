using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.DTOs
{
    public class AvailabilityDTO
    {
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public List<TimeSpan> Hours { get; set; }
    }
}
