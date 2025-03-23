namespace TheMagicParents.Models
{
	public class Support
	{
		public int SupportID { get; set; }
		public string Comment { get; set; }
		public string Status { get; set; }
		public int ComplainerId { get; set; }
		public virtual ICollection<Client> Clients { get; set; }
		public virtual ICollection<ServiceProvider> ServiceProviders { get; set; }


	}
}
