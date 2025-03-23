using System.ComponentModel.DataAnnotations.Schema;

namespace TheMagicParents.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int GovernorateId { get; set; }
        [ForeignKey(nameof(GovernorateId))]
        public virtual Governorate Governorate { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
