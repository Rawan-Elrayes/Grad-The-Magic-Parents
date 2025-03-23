using TheMagicParents.Enums;

namespace TheMagicParents.Models
{
    public class ServiceProvider:User
    {
        public ServiceType Type { get; set; }
        public double Rate { get; set; }
        public string? Certification { get; set; }
        public double HourPrice { get; set; }

		public virtual ICollection<Availability> Availabilities { get; set; }
		public virtual ICollection<Booking> Bookings { get; set; }

	}
}
