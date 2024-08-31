using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ShipToSite
    {
        public int Id { get; set; }
        public int AgencyRegistrationId { get; set; }
        public AgencyRegistration AgencyRegistration { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public string? PhoneExtension { get; set; }
        public string? SiteType { get; set; } // New field
        public string? ShipToName { get; set; }
        public string? ShipToEmail { get; set; }
        public string? ShipToAddress { get; set; }
        public string? ShipToAddress2 { get; set; }
        public string? ShipToCity { get; set; }
        public string? ShipToState { get; set; }
        public string? ShipToZip { get; set; }
        public bool SameAsSite { get; set; }
        public ICollection<ShipToSiteCounty> PrimaryCountiesServed { get; set; } = new List<ShipToSiteCounty>();
        public bool IsEditing { get; set; } // Add this property to track editing state

    }

}
