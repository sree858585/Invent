using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Distributor
{
    public class ManageShipmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageShipmentsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Define the Shipments property
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

        public async Task<IActionResult> OnGetViewShipmentDetailsAsync(int id)
        {
            var order = await _context.Orders
                .Where(o => o.OrderId == id)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.ShipToName,
                    o.ShipToAddress,
                    o.ShipToCity,
                    o.ShipToState,
                    o.ShipToZip,
                    ProgramDirector = _context.AgencyContacts
                                        .Where(ac => ac.AgencyRegistrationId == _context.AgencyRegistrations
                                            .Where(ar => ar.UserId == o.UserId)
                                            .Select(ar => ar.Id).FirstOrDefault())
                                        .Select(ac => ac.ProgramDirector).FirstOrDefault(),
                    Email = _context.AgencyContacts
                                    .Where(ac => ac.AgencyRegistrationId == _context.AgencyRegistrations
                                        .Where(ar => ar.UserId == o.UserId)
                                        .Select(ar => ar.Id).FirstOrDefault())
                                    .Select(ac => ac.Email).FirstOrDefault(),
                    Phone = _context.AgencyContacts
                                    .Where(ac => ac.AgencyRegistrationId == _context.AgencyRegistrations
                                        .Where(ar => ar.UserId == o.UserId)
                                        .Select(ar => ar.Id).FirstOrDefault())
                                    .Select(ac => ac.Phone).FirstOrDefault(),
                    Products = o.OrderDetails.Select(od => new
                    {
                        od.Product.product_description,
                        od.Quantity
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return new JsonResult(new { success = false });
            }

            return new JsonResult(new { success = true, order });
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
