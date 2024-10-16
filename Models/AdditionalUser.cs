using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class AdditionalUser
    {
        public int Id { get; set; }

        public int? PrefixId { get; set; }
        public Prefix Prefix { get; set; }

        public string? Name { get; set; }

        public int? SuffixId { get; set; }
        public Suffix Suffix { get; set; }

        public string? Title { get; set; }
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool SameAddressAsAgency { get; set; }

        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }

        // Foreign Key to EaspSepsRegistration
        [Required]
        public int AgencyRegistrationId { get; set; }
        public AgencyRegistration AgencyRegistration { get; set; }

    }
}
