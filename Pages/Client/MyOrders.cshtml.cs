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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Orders = await _context.Orders
                .Where(o => o.UserId == userId)
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
                    Products = o.OrderDetails.Select(od => new ProductDetail
                    {
                        ProductName = od.Product.product_description,
                        Quantity = od.Quantity
                    }).ToList()
                })
                .ToListAsync();
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
            public List<ProductDetail> Products { get; set; } = new List<ProductDetail>();
        }

        public class ProductDetail
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
        }
    }
}
