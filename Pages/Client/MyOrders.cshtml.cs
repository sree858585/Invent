using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Client
{
    public class MyOrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyOrdersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<OrderViewModel> Orders { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Current user's ID

            // Check if the current user is the main client (not additional user)
            var agencyRegistration = await _context.AgencyRegistrations
                .Include(ar => ar.AgencyContacts)  // Include AgencyContacts to get ProgramDirector
                .FirstOrDefaultAsync(ar => ar.UserId == userId);

            if (agencyRegistration != null)
            {
                // Main client: get their own orders and the orders of additional users under their agency
                var agencyRegistrationId = agencyRegistration.Id;
                var programDirector = agencyRegistration.AgencyContacts.FirstOrDefault()?.ProgramDirector;

                Orders = await _context.Orders
                    .Where(o => o.UserId == userId ||
                                _context.AdditionalUsers.Any(au => au.AgencyRegistrationId == agencyRegistrationId && au.Id == o.AdditionalUserId)) // Fetch orders from main client and additional users
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .Select(o => new OrderViewModel
                    {
                        OrderId = o.OrderId,
                        OrderDate = o.OrderDate,
                        OrderStatus = o.OrderStatus,
                        ApprovedDate = o.ApprovedDate,
                        CanceledDate = o.CanceledDate,
                        ShippedDate = o.ShippedDate,
                        ShipToName = o.ShipToName,
                        ShipToAddress = o.ShipToAddress,
                        ShipToCity = o.ShipToCity,
                        ShipToState = o.ShipToState,
                        ShipToZip = o.ShipToZip,
                        PlacedBy = o.AdditionalUserId == null ? programDirector : _context.AdditionalUsers.FirstOrDefault(au => au.Id == o.AdditionalUserId).Name,  // Use ProgramDirector or AdditionalUserName
                        Products = o.OrderDetails.Select(od => new ProductDetail
                        {
                            ProductName = od.Product.product_description,
                            Quantity = od.Quantity
                        }).ToList()
                    })
                    .ToListAsync();
            }
            else
            {
                // Fetch the additional user based on the current logged-in userId (which is likely a string)
                var additionalUser = await _context.AdditionalUsers
                    .FirstOrDefaultAsync(au => au.Email == User.Identity.Name); // Or use au.UserId == userId if available

                if (additionalUser != null)
                {
                    // Additional user: only see the orders they have placed
                    Orders = await _context.Orders
                        .Where(o => o.AdditionalUserId == additionalUser.Id) // Compare with the AdditionalUser's Id
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                        .Select(o => new OrderViewModel
                        {
                            OrderId = o.OrderId,
                            OrderDate = o.OrderDate,
                            OrderStatus = o.OrderStatus,
                            ApprovedDate = o.ApprovedDate,
                            CanceledDate = o.CanceledDate,
                            ShippedDate = o.ShippedDate,
                            ShipToName = o.ShipToName,
                            ShipToAddress = o.ShipToAddress,
                            ShipToCity = o.ShipToCity,
                            ShipToState = o.ShipToState,
                            ShipToZip = o.ShipToZip,
                            PlacedBy = additionalUser.Name,  // Additional user name as Placed By
                            Products = o.OrderDetails.Select(od => new ProductDetail
                            {
                                ProductName = od.Product.product_description,
                                Quantity = od.Quantity
                            }).ToList()
                        })
                        .ToListAsync();
                }
                else
                {
                    Orders = new List<OrderViewModel>(); // Handle case where no additional user is found
                }
            }
        }


        public string GetProgressBarClass(OrderViewModel order)
        {
            return order.OrderStatus switch
            {
                "ordered" => "bg-info",
                "approved" => "bg-warning",
                "shipped" => "bg-success",
                _ => "bg-secondary",
            };
        }

        public int GetProgressPercentage(OrderViewModel order)
        {
            return order.OrderStatus switch
            {
                "ordered" => 33,
                "approved" => 66,
                "shipped" => 100,
                _ => 0,
            };
        }

        public string GetOrderTimeline(OrderViewModel order)
        {
            return order.OrderStatus switch
            {
                "ordered" => $"Ordered on {order.OrderDate:MM/dd/yyyy}",
                "approved" => $"Ordered on {order.OrderDate:MM/dd/yyyy}, Approved on {order.ApprovedDate:MM/dd/yyyy}",
                "shipped" => $"Ordered on {order.OrderDate:MM/dd/yyyy}, Approved on {order.ApprovedDate:MM/dd/yyyy}, Shipped on {order.ShippedDate:MM/dd/yyyy}",
                _ => $"Status: {order.OrderStatus}"
            };
        }

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public string ShipToName { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
            public string OrderStatus { get; set; }
            public DateTime? ApprovedDate { get; set; }
            public DateTime? CanceledDate { get; set; }
            public DateTime? ShippedDate { get; set; }
            public string PlacedBy { get; set; }  // Added property for Order Placed By
            public List<ProductDetail> Products { get; set; } = new List<ProductDetail>();
        }

        public class ProductDetail
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
        }
    }
}
