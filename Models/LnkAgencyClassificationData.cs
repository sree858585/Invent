using System;

namespace WebApplication1.Models
{
    public class LnkAgencyClassificationData
    {
        public int Id { get; set; }
        public int Category { get; set; }
        public bool Other { get; set; }
        public int AgencyRegistrationId { get; set; }
        public string OtherClassificationText { get; set; }
        public string? UniqueId { get; set; } // Ensure this property is nullable

        public AgencyRegistration AgencyRegistration { get; set; }
    }
}
