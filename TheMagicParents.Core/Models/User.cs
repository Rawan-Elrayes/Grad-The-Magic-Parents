using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using TheMagicParents.Enums;

namespace TheMagicParents.Models
{
    public class User:IdentityUser
    {
        public string? PersonalPhoto { get; set; }
        public string? IdCardFrontPhoto { get; set; }
        public string? IdCardBackPhoto { get;set; }
        public string? PersonWithCard { get; set; }
        public StateType? AccountState { get; set; } = StateType.New;
        public int? NumberOfSuccessfulServices { get; set; } = 0;
        public int? NumberOfCanceledServices { get; set; } = 0;
        public int? NumberOfSupports { get; set; } = 0;
        public string? UserNameId { get; set; } 
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public int? CityId { get; set; }
		[ForeignKey(nameof(CityId))]
		public virtual City City { get; set; }

        public int? SupportId { get; set; }
        [ForeignKey(nameof(SupportId))]
        public virtual Support support { get; set; }
    }
}
