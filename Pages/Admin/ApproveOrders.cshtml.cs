using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin
{
    public class ApproveOrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10;

        public ApproveOrdersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<OrderViewModel> PendingOrders { get; set; }
        public List<OrderViewModel> ApprovedOrders { get; set; }
        public List<OrderViewModel> CancelledOrders { get; set; }
        public List<OrderViewModel> ShippedOrders { get; set; } // Add this property for shipped orders
        public int PendingOrdersPageNumber { get; set; }
        public int PendingOrdersTotalPages { get; set; }
        public int ApprovedOrdersPageNumber { get; set; }
        public int ApprovedOrdersTotalPages { get; set; }
        public int CancelledOrdersPageNumber { get; set; }
        public int CancelledOrdersTotalPages { get; set; }
        public int ShippedOrdersPageNumber { get; set; }
        public int ShippedOrdersTotalPages { get; set; }
        public async Task OnGetAsync(int pendingPage = 1, int approvedPage = 1, int cancelledPage = 1, int shippedPage = 1, string currentTab = "pending")
        {
            ViewData["CurrentTab"] = currentTab;

            var ordersQuery = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Join(
                    _context.AgencyRegistrations,
                    order => order.UserId,
                    agency => agency.UserId,
                    (order, agency) => new { Order = order, Agency = agency }
                )
                .GroupBy(
                    o => o.Order.OrderId
                )
                .Select(group => new OrderViewModel
                {
                    OrderId = group.First().Order.OrderId,
                    AgencyName = group.First().Agency.AgencyName,
                    OrderDate = group.First().Order.OrderDate,
                    OrderStatus = group.First().Order.OrderStatus,
                    ApprovedDate = group.First().Order.ApprovedDate,
                    CanceledDate = group.First().Order.CanceledDate,
                    ShippedDate = group.First().Order.ShippedDate,
                    ShipToName = group.First().Order.ShipToName,
                    ShipToAddress = group.First().Order.ShipToAddress,
                    ShipToCity = group.First().Order.ShipToCity,
                    ShipToState = group.First().Order.ShipToState,
                    ShipToZip = group.First().Order.ShipToZip,
                    Products = group.First().Order.OrderDetails.Select(od => new ProductDetail
                    {
                        ProductName = od.Product.product_description,
                        Quantity = od.Quantity
                    }).ToList()
                });

            // Pending Orders
            PendingOrdersPageNumber = pendingPage;
            PendingOrdersTotalPages = (int)Math.Ceiling(await ordersQuery.CountAsync(o => o.OrderStatus == "ordered") / (double)PageSize);
            PendingOrders = await ordersQuery
                .Where(o => o.OrderStatus == "ordered")
                .Skip((pendingPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Approved Orders
            ApprovedOrdersPageNumber = approvedPage;
            ApprovedOrdersTotalPages = (int)Math.Ceiling(await ordersQuery.CountAsync(o => o.OrderStatus == "approved") / (double)PageSize);
            ApprovedOrders = await ordersQuery
                .Where(o => o.OrderStatus == "approved")
                .Skip((approvedPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Cancelled Orders
            CancelledOrdersPageNumber = cancelledPage;
            CancelledOrdersTotalPages = (int)Math.Ceiling(await ordersQuery.CountAsync(o => o.OrderStatus == "canceled") / (double)PageSize);
            CancelledOrders = await ordersQuery
                .Where(o => o.OrderStatus == "canceled")
                .Skip((cancelledPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Shipped Orders
            ShippedOrdersPageNumber = shippedPage;
            ShippedOrdersTotalPages = (int)Math.Ceiling(await ordersQuery.CountAsync(o => o.OrderStatus == "shipped") / (double)PageSize);
            ShippedOrders = await ordersQuery
                .Where(o => o.OrderStatus == "shipped")
                .Skip((shippedPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }


        public async Task<IActionResult> OnPostApproveOrderAsync([FromBody] OrderRequest request)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order != null && (order.OrderStatus == "ordered" || order.OrderStatus == "canceled"))
            {
                order.OrderStatus = "approved";
                order.ApprovedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false });
        }


        public async Task<IActionResult> OnPostCancelOrderAsync([FromBody] OrderRequest request)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order != null && (order.OrderStatus == "ordered" || order.OrderStatus == "approved"))
            {
                order.OrderStatus = "canceled";
                order.CanceledDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            return new JsonResult(new { success = false });
        }

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string AgencyName { get; set; }
            public DateTime OrderDate { get; set; }
            public string OrderStatus { get; set; }
            public DateTime? ApprovedDate { get; set; }
            public DateTime? CanceledDate { get; set; }
            public DateTime? ShippedDate { get; set; }
            public string ShipToName { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
            public List<ProductDetail> Products { get; set; } = new List<ProductDetail>();
            public List<string> UniqueIds { get; set; } = new List<string>(); // Add this for displaying Unique IDs
        }

        public class ProductDetail
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
        }

        public class OrderRequest
        {
            public int OrderId { get; set; }
        }
    }
}
