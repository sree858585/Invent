using System;
namespace WebApplication1.Models
{
    public class AgentClassificationData
    {
        public int Id { get; set; }
        public int Category { get; set; }
        public bool Other { get; set; }
        public int EaspSepsRegistrationID { get; set; }
        public string OtherClassificationText { get; set; }  


        public EaspSepsRegistration EaspSepsRegistration { get; set; }
    }

}

