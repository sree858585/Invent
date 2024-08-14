using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using System;

namespace WebApplication1.Pages.Client
{
    public class HomeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public HomeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public OrderViewModel LatestOrder { get; set; }
        public RegistrationViewModel LatestRegistration { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Retrieve the latest order
            LatestOrder = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate,
                    OrderStatus = o.OrderStatus,
                    ShipToName = o.ShipToName,
                    ShipToAddress = o.ShipToAddress,
                    ShipToCity = o.ShipToCity,
                    ShipToState = o.ShipToState,
                    ShipToZip = o.ShipToZip,
                    ApprovedDate = o.ApprovedDate,
                    CanceledDate = o.CanceledDate,
                    ShippedDate = o.ShippedDate
                })
                .FirstOrDefaultAsync();

            // Retrieve the latest registration
            LatestRegistration = await _context.AgencyRegistrations
     .Where(r => r.UserId == userId)
     .OrderByDescending(r => r.SubmissionDate)
     .Select(r => new RegistrationViewModel
     {
         AgencyName = r.AgencyName,
         RegistrationDate = r.SubmissionDate,
         Status = r.Status,
         RegistrationType = r.RegistrationType,
         UniqueIds = r.LnkAgencyClassificationData
            .Where(c => c.Category == 1 || c.Category == 2 || c.Category == 3)
            .Select(c => c.UniqueId)
            .ToList() // Ensure we collect all UniqueIds as a list
     })
    .FirstOrDefaultAsync();


        }

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string OrderStatus { get; set; }
            public string ShipToName { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
            public DateTime OrderDate { get; set; }
            public DateTime? ApprovedDate { get; set; }
            public DateTime? CanceledDate { get; set; }
            public DateTime? ShippedDate { get; set; }
        }

        public class RegistrationViewModel
        {
            public string AgencyName { get; set; }
            public DateTime RegistrationDate { get; set; }
            public string Status { get; set; }
            public string RegistrationType { get; set; }
            public List<string> UniqueIds { get; set; } // Change from string to List<string>
        }
    }
}
