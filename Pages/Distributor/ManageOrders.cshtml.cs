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
                .Join(_context.AgencyContacts,
                      reg => reg.registration.Id,
                      contact => contact.AgencyRegistrationId,
                      (reg, contact) => new { reg.order, reg.registration, contact })
                .Where(o => o.order.OrderStatus == "approved")
                .ToListAsync();

            Orders = ordersData
                .GroupBy(o => o.order.OrderId)
                .Select(g => new OrderViewModel
                {
                    OrderId = g.Key,
                    OrderDate = g.First().order.OrderDate,
                    ApprovedDate = g.First().order.ApprovedDate,
                    OrderStatus = g.First().order.OrderStatus,
                    ShipToName = g.First().order.ShipToName,
                    AgencyName = g.First().registration.AgencyName,
                    ProgramDirector = g.First().contact.ProgramDirector,
                    Email = g.First().contact.Email,
                    Phone = g.First().contact.Phone,
                    UserId = g.First().order.UserId
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
            if (request == null || request.OrderId <= 0 || request.ProductId <= 0 || request.Quantity <= 0)
            {
                return new JsonResult(new { success = false });
            }

            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null)
            {
                return new JsonResult(new { success = false });
            }

            var existingOrderDetail = await _context.OrderDetails
                .FirstOrDefaultAsync(od => od.OrderId == request.OrderId && od.product_id == request.ProductId);

            if (existingOrderDetail != null)
            {
                existingOrderDetail.Quantity += request.Quantity;
            }
            else
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = request.OrderId,
                    product_id = request.ProductId,
                    Quantity = request.Quantity
                };

                _context.OrderDetails.Add(orderDetail);
            }

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

        public async Task<IActionResult> OnPostGetAvailableProductsAsync()
        {
            var products = await _context.Products
                .Where(p => p.is_active)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.product_id,
                    ProductName = p.product_description
                })
                .ToListAsync();

            return new JsonResult(new { success = true, products = products }); // Ensure 'products' is an array
        }


        public async Task<IActionResult> OnPostGetOrderHistoryAsync([FromBody] UserRequest request)
        {
            var orderHistory = await _context.Orders
                .Where(o => o.UserId == request.UserId)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Join(_context.AgencyRegistrations,
                      order => order.UserId,
                      registration => registration.UserId,
                      (order, registration) => new { order, registration })
                .Join(_context.AgencyContacts,
                      reg => reg.registration.Id,
                      contact => contact.AgencyRegistrationId,
                      (reg, contact) => new { reg.order, reg.registration, contact })
                .OrderByDescending(o => o.order.ShippedDate)
                .Select(o => new OrderHistoryViewModel
                {
                    OrderId = o.order.OrderId,
                    ShipDate = o.order.ShippedDate,
                    OrderStatus = o.order.OrderStatus,
                    AgencyName = o.registration.AgencyName,
                    ProgramDirector = o.contact.ProgramDirector,
                    Email = o.contact.Email,
                    Phone = o.contact.Phone,
                    ShipToAddress = new ShippingInfoViewModel
                    {
                        ShipToName = o.order.ShipToName,
                        ShipToAddress = o.order.ShipToAddress,
                        ShipToAddress2 = o.order.ShipToAddress2,
                        ShipToCity = o.order.ShipToCity,
                        ShipToState = o.order.ShipToState,
                        ShipToZip = o.order.ShipToZip
                    },
                    Products = o.order.OrderDetails.Select(od => new ProductViewModel
                    {
                        ProductName = od.Product.product_description,
                        Quantity = od.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return new JsonResult(new { success = true, history = orderHistory });
        }

        public async Task<IActionResult> OnPostSaveOrderAsync([FromBody] SaveOrderRequest request)
        {
            if (request == null || request.OrderId <= 0 || request.UpdatedProducts == null)
            {
                return new JsonResult(new { success = false });
            }

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

        // ViewModel classes
        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string OrderStatus { get; set; }
            public string ShipToName { get; set; }
            public DateTime OrderDate { get; set; }
            public DateTime? ApprovedDate { get; set; }
            public string AgencyName { get; set; }
            public string RegistrationType { get; set; }
            public string ProgramDirector { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string UserId { get; set; } // Added UserId property
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
            public string ShipToName { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
        }

        public class OrderHistoryViewModel
        {
            public int OrderId { get; set; }
            public DateTime? ShipDate { get; set; } // Replacing OrderDate with ShipDate
            public string OrderStatus { get; set; }
            public string AgencyName { get; set; }
            public string ProgramDirector { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public ShippingInfoViewModel ShipToAddress { get; set; } // Shipping Address Details
            public List<ProductViewModel> Products { get; set; } // List of Products
        }

        public class ShippingInfoViewModel
        {
            public string ShipToName { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToAddress2 { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
        }

        public class UserRequest
        {
            public string UserId { get; set; }
        }
    }
}
