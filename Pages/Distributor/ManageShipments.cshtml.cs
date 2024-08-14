using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
namespace WebApplication1.Pages.Distributor
{
    [Authorize(Roles = "Distributor")]
    public class ManageShipmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageShipmentsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ShipmentViewModel> Shipments { get; set; }

        public async Task OnGetAsync()
        {
            Shipments = await _context.Orders
                .Where(o => o.OrderStatus == "shipped")
                .Select(o => new ShipmentViewModel
                {
                    ShipmentId = o.OrderId, // Assuming OrderId is used as ShipmentId
                    OrderId = o.OrderId,
                    ShipToName = o.ShipToName,
                    ShippedDate = o.ShippedDate ?? DateTime.Now // Replace with actual ShippedDate
                })
                .ToListAsync();
        }

        public class ShipmentViewModel
        {
            public int ShipmentId { get; set; }
            public int OrderId { get; set; }
            public string ShipToName { get; set; }
            public DateTime ShippedDate { get; set; }
        }
    }

}
