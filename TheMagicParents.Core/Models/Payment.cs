using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TheMagicParents.Enums;

namespace TheMagicParents.Models
{
	public class Payment
	{
		[Key]
		public int PaymentID { get; set; }
		[Required]
		public decimal Amount { get; set; }
		public DateTime PaymentDate { get; set; }
		public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotInitiated;
		public PaymentMethodType PaymentMethod { get; set; } = PaymentMethodType.BankTransfer;
		public int BookingID { get; set; }

		[ForeignKey(nameof(BookingID))]
		public virtual Booking Booking { get; set; }


	}
}
