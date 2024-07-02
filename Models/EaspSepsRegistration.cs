using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class EaspSepsRegistration
    {
        public int Id { get; set; }
        [Required]
        public string AgencyName { get; set; }
        public string AlternateName { get; set; }
        [Required]
        public string County { get; set; }
        [Required]
        public string UniqueId { get; set; }
        [Required]
        public string Address { get; set; }
        public string Address2 { get; set; }
        [Required]
        public string City { get; set; }
        public string State { get; set; } = "NY";
        public string Zip { get; set; }
        [Required]
        public string RegistrationType { get; set; }

        // Foreign Key to ApplicationUser
        [Required]
        public string UserId { get; set; }
        public List<EaspSepsRegistrationCounty> CountiesServed { get; set; } = new List<EaspSepsRegistrationCounty>(); // Updated property


    }

}
