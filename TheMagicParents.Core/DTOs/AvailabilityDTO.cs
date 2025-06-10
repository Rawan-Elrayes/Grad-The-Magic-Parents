using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.DTOs
{
    public class AvailabilityDTO
    {
        [Required]
        public DateTime Date { get; set; }

        //[AllowNull]
        public List<TimeSpan>? Hours { get; set; }
    }
}
