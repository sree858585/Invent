using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Distributor
{
    public class ManageOrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageOrdersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<OrderViewModel> Orders { get; set; }

        public async Task OnGetAsync()
        {
            var ordersData = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Join(_context.AgencyRegistrations,
                      order => order.UserId,
                      registration => registration.UserId,
                      (order, registration) => new { order, registration })
                .Where(o => o.order.OrderStatus == "approved")
                .ToListAsync();

            Orders = ordersData
                .GroupBy(o => o.order.OrderId)
                .Select(g => new OrderViewModel
                {
                    OrderId = g.Key,
                    OrderDate = g.First().order.OrderDate,
                    OrderStatus = g.First().order.OrderStatus,
                    ShipToName = g.First().order.ShipToName,
                    AgencyName = g.First().registration.AgencyName,
                    RegistrationType = g.First().registration.RegistrationType
                })
                .ToList();
        }

        public async Task<IActionResult> OnPostGetOrderDetailsAsync([FromBody] OrderRequest request)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null)
            {
                return new JsonResult(new { success = false });
            }

            var orderDetailsViewModel = new OrderDetailsViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                ShipToName = order.ShipToName,
                ShipToAddress = order.ShipToAddress,
                ShipToCity = order.ShipToCity,
                ShipToState = order.ShipToState,
                ShipToZip = order.ShipToZip,
                Products = order.OrderDetails.Select(od => new ProductViewModel
                {
                    ProductId = od.product_id,
                    ProductName = od.Product.product_description,
                    Quantity = od.Quantity
                }).ToList()
            };

            return new JsonResult(new { success = true, order = orderDetailsViewModel });
        }


        // Handler to mark the order as shipped
        public async Task<IActionResult> OnPostMarkAsShippedAsync([FromBody] OrderRequest request)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null || order.OrderStatus != "approved")
            {
                return new JsonResult(new { success = false });
            }

            order.OrderStatus = "shipped";
            order.ShippedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRemoveProductAsync([FromBody] ProductRequest request)
        {
            var orderDetail = await _context.OrderDetails
                .FirstOrDefaultAsync(od => od.OrderId == request.OrderId && od.product_id == request.ProductId);

            if (orderDetail == null)
            {
                return new JsonResult(new { success = false });
            }

            _context.OrderDetails.Remove(orderDetail);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostAddProductAsync([FromBody] ProductRequest request)
        {
            var orderDetail = new OrderDetail
            {
                OrderId = request.OrderId,
                product_id = request.ProductId,
                Quantity = request.Quantity
            };

            _context.OrderDetails.Add(orderDetail);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostUpdateShippingAsync([FromBody] ShippingUpdateRequest request)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null)
            {
                return new JsonResult(new { success = false });
            }

            order.ShipToName = request.ShipToName;
            order.ShipToAddress = request.ShipToAddress;
            order.ShipToCity = request.ShipToCity;
            order.ShipToState = request.ShipToState;
            order.ShipToZip = request.ShipToZip;

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostSaveOrderAsync([FromBody] SaveOrderRequest request)
        {
            if (request == null || request.OrderId <= 0 || request.UpdatedProducts == null)
            {
                return new JsonResult(new { success = false });
            }

            foreach (var product in request.UpdatedProducts)
            {
                var orderDetail = await _context.OrderDetails
                    .FirstOrDefaultAsync(od => od.OrderId == request.OrderId && od.product_id == product.ProductId);

                if (orderDetail != null)
                {
                    orderDetail.Quantity = product.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }


        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string OrderStatus { get; set; }
            public string ShipToName { get; set; }
            public DateTime OrderDate { get; set; }
            public string AgencyName { get; set; }
            public string RegistrationType { get; set; }
        }

        public class OrderDetailsViewModel
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public string ShipToName { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
            public List<ProductViewModel> Products { get; set; }
        }

        public class ProductViewModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
        }

        public class ProductUpdateModel
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        // Request classes
        public class OrderRequest
        {
            public int OrderId { get; set; }
        }

        public class ProductRequest
        {
            public int OrderId { get; set; }
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class ShippingUpdateRequest
        {
            public int OrderId { get; set; }
            public string ShipToName { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
        }

        public class SaveOrderRequest
        {
            public int OrderId { get; set; }
            public List<ProductUpdateModel> UpdatedProducts { get; set; }
        }
    }
}
