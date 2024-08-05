using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Client
{
    public class PlaceOrderModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PlaceOrderModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string Classification1ProductsJson { get; set; }
        public string Classification2ProductsJson { get; set; }
        public string ShipInfoJson { get; set; }

        public bool CanAccessClassification1 { get; set; }
        public bool CanAccessClassification2 { get; set; }

        [BindProperty]
        public ShipInformation ShipInfo { get; set; }

        [BindProperty]
        public List<CartItem> Cart { get; set; }

        public class CartItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public async Task OnGetAsync()
        {
            var email = User.Identity.Name;

            // Retrieve the user based on the email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Check if the user was found and retrieve the userId
            if (user == null)
            {
                CanAccessClassification1 = false;
                CanAccessClassification2 = false;
                return;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userRegistrations = await _context.AgencyRegistrations
                .Where(ar => ar.UserId == userId && ar.Status == "Approved")
                .Include(ar => ar.LnkAgencyClassificationData)
                .Include(ar => ar.ShipInformations)
                .ToListAsync();

            if (!userRegistrations.Any())
            {
                CanAccessClassification1 = false;
                CanAccessClassification2 = false;
                return;
            }

            CanAccessClassification1 = userRegistrations.Any(reg => reg.LnkAgencyClassificationData.Any(acd => acd.Category == 1));
            CanAccessClassification2 = userRegistrations.Any(reg => reg.LnkAgencyClassificationData.Any(acd => acd.Category == 2 || acd.Category == 3));

            if (CanAccessClassification1)
            {
                var classification1Products = await _context.Products
                    .Where(p => p.product_agency_type == 1 && p.is_active)
                    .Select(p => new
                    {
                        p.product_id,
                        p.product_item_num,
                        p.product_description,
                        p.product_pieces_per_case,
                        p.is_active
                    })
                    .ToListAsync();
                Classification1ProductsJson = JsonConvert.SerializeObject(classification1Products);
            }

            if (CanAccessClassification2)
            {
                var classification2Products = await _context.Products
                    .Where(p => p.product_agency_type == 2 && p.is_active)
                    .Select(p => new
                    {
                        p.product_id,
                        p.product_item_num,
                        p.product_description,
                        p.product_pieces_per_case,
                        p.is_active
                    })
                    .ToListAsync();
                Classification2ProductsJson = JsonConvert.SerializeObject(classification2Products);
            }

            var firstApprovedRegistration = userRegistrations.FirstOrDefault();
            if (firstApprovedRegistration?.ShipInformations.Any() == true)
            {
                var shipInfo = firstApprovedRegistration.ShipInformations.First();
                ShipInfoJson = JsonConvert.SerializeObject(new
                {
                    shipInfo.ShipToName,
                    shipInfo.ShipToEmail,
                    shipInfo.ShipToAddress,
                    shipInfo.ShipToAddress2,
                    shipInfo.ShipToCity,
                    shipInfo.ShipToState,
                    shipInfo.ShipToZip
                });
            }
        }

        public async Task<IActionResult> OnPostCheckoutAsync([FromBody] CheckoutRequest request)
        {
            System.Diagnostics.Debug.WriteLine("OnPostCheckoutAsync method called");

            // Log the incoming request body for debugging
            var requestBody = JsonConvert.SerializeObject(request);
            System.Diagnostics.Debug.WriteLine("Incoming request: " + requestBody);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                System.Diagnostics.Debug.WriteLine("Model State Errors: " + string.Join(", ", errors));
                return new JsonResult(new { success = false, message = "Invalid request data.", errors });
            }

            if (request.Cart == null || !request.Cart.Any())
            {
                System.Diagnostics.Debug.WriteLine("Cart is empty.");
                return new JsonResult(new { success = false, message = "Cart is empty." });
            }

            var email = User.Identity.Name;
            System.Diagnostics.Debug.WriteLine("User email: " + email);

            // Retrieve the user based on the email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("User not found.");
                return new JsonResult(new { success = false, message = "User not found." });
            }

            var userId = user.Id;
            System.Diagnostics.Debug.WriteLine("User ID: " + userId);

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                OrderStatus = "ordered",
                ShipToName = request.ShippingInfo.ShipToName,
                ShipToEmail = request.ShippingInfo.ShipToEmail,
                ShipToAddress = request.ShippingInfo.ShipToAddress,
                ShipToAddress2 = request.ShippingInfo.ShipToAddress2,
                ShipToCity = request.ShippingInfo.ShipToCity,
                ShipToState = request.ShippingInfo.ShipToState,
                ShipToZip = request.ShippingInfo.ShipToZip
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in request.Cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                };
                _context.OrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine("Order placed successfully. Order ID: " + order.OrderId);

            return new JsonResult(new { success = true, orderId = order.OrderId });
        }

        public class CheckoutRequest
        {
            public List<CartItem> Cart { get; set; }
            public ShippingInformation ShippingInfo { get; set; }
        }

        public class ShippingInformation
        {
            public string ShipToName { get; set; }
            public string ShipToEmail { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToAddress2 { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
        }
    }
}
