using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace WebApplication1.Models
{
    public class AgentClassificationData
    {
        public int Id { get; set; }
        public int Category { get; set; }
        public bool Other { get; set; }
        public int AgencyRegistrationID { get; set; }
        public string OtherClassificationText { get; set; }
        public string? UniqueId { get; set; } // Ensure this property is nullable

        public AgencyRegistration AgencyRegistration { get; set; }
    }

}

