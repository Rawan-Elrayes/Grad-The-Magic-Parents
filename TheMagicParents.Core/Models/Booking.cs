using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TheMagicParents.Enums;

namespace TheMagicParents.Models
{
	public class Booking
	{
		[Key]
		public int BookingID { get; set; }
		public string ClientId { get; set; }
		public string ServiceProviderID { get; set; }
		[Required]
		public DateTime Day { get; set; }
		public TimeSpan Houre { get; set; }

		[Required]
		public double TotalPrice { get; set; }
		public BookingStatus Status { get; set; } = BookingStatus.pending;
		public string Location { get; set; }
		public string? cancelledBy { get; set; }

		// Navigation properties
		[ForeignKey(nameof(ClientId))]
		public virtual Client Client { get; set; }
		[ForeignKey(nameof(ServiceProviderID))]
		public virtual ServiceProvider ServiceProvider { get; set; }

		//1:1 relations
		public virtual Payment Payment { get; set; }
		public virtual Review Review { get; set; }
	}
}
