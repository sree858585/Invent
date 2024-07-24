using System;
namespace WebApplication1.Models
{
    public class EaspSepsRegistrationCounty
    {
        //public int Id { get; set; }
        public int AgencyRegistrationId { get; set; }
        public AgencyRegistration AgencyRegistration { get; set; }
        public int CountyId { get; set; }
        public County County { get; set; }
    }

}

