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
		public DateTime BookingTime { get; set; }
		[Required]
		public decimal TotalPrice { get; set; }
		public BookingStatus Status { get; set; } = BookingStatus.none;
		public string Location { get; set; }
		public int CityID { get; set; }

		// Navigation properties
		[ForeignKey(nameof(ClientId))]
		public virtual Client Client { get; set; }
		[ForeignKey(nameof(ServiceProviderID))]
		public virtual ServiceProvider ServiceProvider { get; set; }
		[ForeignKey(nameof(CityID))]
		public virtual City City { get; set; }

		//1:1 relations
		public virtual Payment Payment { get; set; }
		public virtual Review Review { get; set; }
	}
}
