using System;
namespace WebApplication1.Models
{
    public class EaspSepsRegistrationCounty
    {
        //public int Id { get; set; }
        public int EaspSepsRegistrationId { get; set; }
        public EaspSepsRegistration EaspSepsRegistration { get; set; }
        public int CountyId { get; set; }
        public County County { get; set; }
    }

}

