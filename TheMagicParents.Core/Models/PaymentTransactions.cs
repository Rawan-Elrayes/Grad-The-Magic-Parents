using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Transactions;
using TheMagicParents.Enums;

namespace TheMagicParents.Models
{
	public class PaymentTransactions
	{
        [Key]
        public int TransactionID { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // Bank/card information
        [Required]
        public string AccountNumber { get; set; }  // Consider encryption

        public string BankName { get; set; }

        [Required]
        public string ServiceProviderID { get; set; }

        [ForeignKey(nameof(ServiceProviderID))]
        public virtual ServiceProvider ServiceProvider { get; set; }
    }
}
