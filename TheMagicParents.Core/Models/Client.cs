namespace TheMagicParents.Models
{
    public class Client:User
    {
        public string Location { get; set; }
		public virtual ICollection<Booking> Bookings { get; set; }
	}
}
