using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class AgencyContact
    {
        public int Id { get; set; }
        public string ProgramDirector { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
        public string PhoneExtension { get; set; }
        public string AltPhone { get; set; }
        public string AltPhoneExtension { get; set; }
        public string Email { get; set; }

        public int? SuffixId { get; set; }  // Update here
        public Suffix Suffix { get; set; }  // Navigation property

        // Change to SameAddressAsAgency
        public bool SameAddressAsAgency { get; set; }

        // Foreign Key to EaspSepsRegistration
        [Required]
        public int EaspSepsRegistrationId { get; set; }
    }
}

