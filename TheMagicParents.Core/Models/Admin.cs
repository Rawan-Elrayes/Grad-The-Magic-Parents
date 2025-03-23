using System.ComponentModel.DataAnnotations;

namespace TheMagicParents.Models
{
	public class Admin
	{
		[Key]
		[EmailAddress]
		public string Email { get; set; }
		[Required]
		public string Password { get; set; }

	}
}
