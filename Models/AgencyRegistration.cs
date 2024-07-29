using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class AgencyRegistration
    {
        public int Id { get; set; }
        [Required]
        public string AgencyName { get; set; }
        public string AlternateName { get; set; }
        [Required]
        public string County { get; set; }
        [Required]
        public string Address { get; set; }
        public string Address2 { get; set; }
        [Required]
        public string City { get; set; }
        public string State { get; set; } = "NY";
        public string Zip { get; set; }
        [Required]
        public string RegistrationType { get; set; }
        [Required]
        public string UserId { get; set; }
        public List<EaspSepsRegistrationCounty> CountiesServed { get; set; } = new List<EaspSepsRegistrationCounty>();
        public ICollection<ShipToSite> ShipToSites { get; set; }
        public ICollection<LnkAgencyClassificationData> LnkAgencyClassificationData { get; set; } // Updated property name
        public ICollection<AgencyContact> AgencyContacts { get; set; } // Corrected to a collection
        public ICollection<AdditionalUser> AdditionalUsers { get; set; } // Corrected to a collection
        public ICollection<ShipInformation> ShipInformations { get; set; } // Corrected to a collection


        public DateTime SubmissionDate { get; set; } // New property for submission date
        public string Status { get; set; } = "Pending";  // Default value for status

    }


}