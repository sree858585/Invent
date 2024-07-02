using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class AdditionalUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; } // Add this line
        public string City { get; set; }
        public string State { get; set; } // Add this line
        public string Zip { get; set; } // Add this line
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool SameAddressAsAgency { get; set; } // Add this line

        [Required]
        public int EaspSepsRegistrationId { get; set; }
    }
}

