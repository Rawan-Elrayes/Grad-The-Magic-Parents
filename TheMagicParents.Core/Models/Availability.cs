using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TheMagicParents.Models
{
	public class Availability
	{
		public int Id { get; set; }
		//Handle that each day has certain times
		[Required]
		public DateTime Date { get; set; }
		[Required]
		public TimeSpan Time { get; set; } 
		public string ServiceProciderID { get; set; }

		[ForeignKey("ServiceProciderID")]
		public virtual ServiceProvider ServiceProvider { get; set; }

	}
}
