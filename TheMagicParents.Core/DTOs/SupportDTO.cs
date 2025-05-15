using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMagicParents.Core.DTOs
{
    public class SupportDTO
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [MinLength(10, ErrorMessage = "Report reason must be at least 10 characters")]
        [MaxLength(500, ErrorMessage = "Report reason cannot exceed 500 characters")]
        public string Comment { get; set; }
    }
}
