using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ShipInformation
    {
        public int Id { get; set; }
        public string ShipToName { get; set; }
        public string ShipToEmail { get; set; }
        public string ShipToAddress { get; set; }
        public string ShipToAddress2 { get; set; }
        public string ShipToCity { get; set; }
        public string ShipToState { get; set; }
        public string ShipToZip { get; set; }
        public bool SameAsSite { get; set; }

        [Required]
        public int AgencyRegistrationId { get; set; }
    }
}
