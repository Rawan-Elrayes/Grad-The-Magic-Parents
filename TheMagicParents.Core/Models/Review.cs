using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TheMagicParents.Models
{
	public class Review //1:1 with booking .
	{
		[Key]
		public int ReviewID { get; set; }

		[Required]
		[Range(1, 5)]
		public int Rating { get; set; }
		public DateTime ReviewDate { get; set; }
		public int BookingID { get; set; }

		[ForeignKey(nameof(BookingID))]
		public virtual Booking Booking { get; set; }
	}
}
