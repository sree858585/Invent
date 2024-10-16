using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace WebApplication1.Models
{
    public class ApplicationUser : IdentityUser
    {
       // public string Id { get; set; }

        public string Role { get; set; } // "Client", "Admin", "Distributor" , "AdditionalUser"
        public bool IsApproved { get; set; } // For Admin approval
    }
}
