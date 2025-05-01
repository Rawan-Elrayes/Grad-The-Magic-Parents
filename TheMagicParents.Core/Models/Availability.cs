using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TheMagicParents.Models
{
	public class Availability
	{
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public TimeSpan StartTime { get; set; } 
		public TimeSpan EndTime { get; set; } 
		public string ServiceProciderID { get; set; }

		[ForeignKey("ServiceProciderID")]
		public virtual ServiceProvider ServiceProvider { get; set; }

	}
}
