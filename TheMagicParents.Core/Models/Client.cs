namespace TheMagicParents.Models
{
    public class Client:User
    {
        public int Points { get; set; }
        public string Location { get; set; }
		public virtual ICollection<Booking> Bookings { get; set; }
	}
}
