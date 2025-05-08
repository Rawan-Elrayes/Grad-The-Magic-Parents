using System.ComponentModel.DataAnnotations;

namespace TheMagicParents.Models
{
	public class Support
	{
		[Key]
		public int SupportID { get; set; }
		public string Comment { get; set; }
		public string Status { get; set; }
		public string ComplainerId { get; set; }

		//1:1
		public virtual User user { get; set; }

	}
}
