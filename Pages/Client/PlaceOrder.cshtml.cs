using Microsoft.AspNetCore.Authorization;
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
using WebApplication1.Services;

namespace WebApplication1.Pages.Client
{
    [Authorize(Roles = "Client,AdditionalUser")]
    public class PlaceOrderModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly GeocodingService _geocodingService;


        public PlaceOrderModel(ApplicationDbContext context, IEmailService emailService, GeocodingService geocodingService)
        {
            _context = context;
            _emailService = emailService;
            _geocodingService = geocodingService;

        }

        public string Classification1ProductsJson { get; set; }
        public string Classification2ProductsJson { get; set; }

        public bool CanAccessClassification1 { get; set; }
        public bool CanAccessClassification2 { get; set; }

        [BindProperty]
        public List<CartItem> Cart { get; set; }

        public class CartItem
        {
            public int product_id { get; set; }
            public int Quantity { get; set; }
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

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = User.FindFirstValue(ClaimTypes.Role);

            // Check if it's a main user or additional user
            if (userRoles.Contains("AdditionalUser"))
            {
                // Fetch the main user's ID from the additional user's agency registration
                var additionalUser = await _context.AdditionalUsers
                    .Include(a => a.AgencyRegistration)
                    .FirstOrDefaultAsync(a => a.Email == User.Identity.Name);

                if (additionalUser != null && additionalUser.AgencyRegistration != null)
                {
                    userId = additionalUser.AgencyRegistration.UserId;  // Main user's ID
                }
            }

            var userRegistrations = await _context.AgencyRegistrations
                .Where(ar => ar.UserId == userId && ar.Status == "Approved")
                .Include(ar => ar.LnkAgencyClassificationData)
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
        }

        public async Task<IActionResult> OnPostCheckoutAsync([FromBody] CheckoutRequest request)
        {
            try
            {
                if (!ModelState.IsValid || request.Cart == null || !request.Cart.Any())
                {
                    //return new JsonResult(new { success = false, message = "Invalid request data or cart is empty." });
                }

                // Retrieve User ID from Claims
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Main client ID
                int? additionalUserId = null;  // Nullable int for additional user ID

                if (string.IsNullOrEmpty(userId))
                {
                    throw new Exception("User ID is null or empty. Unable to place order.");
                }

                // Check if the user is an additional user
                var userRoles = User.FindFirstValue(ClaimTypes.Role);
                if (userRoles.Contains("AdditionalUser"))
                {
                    var additionalUser = await _context.AdditionalUsers
                        .Include(au => au.AgencyRegistration)
                        .FirstOrDefaultAsync(au => au.Email == User.Identity.Name);

                    if (additionalUser != null && additionalUser.AgencyRegistration != null)
                    {
                        userId = additionalUser.AgencyRegistration.UserId;  // Use the main client's ID
                        additionalUserId = additionalUser.Id;  // Set the additional user's ID
                    }
                    else
                    {
                        throw new Exception("Additional user is not associated with any agency registration.");
                    }
                }

                // Ensure all required fields are set
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(request.ShippingInfo.ShipToName) ||
                    string.IsNullOrEmpty(request.ShippingInfo.ShipToEmail) || string.IsNullOrEmpty(request.ShippingInfo.ShipToAddress) ||
                    string.IsNullOrEmpty(request.ShippingInfo.ShipToCity) || string.IsNullOrEmpty(request.ShippingInfo.ShipToState) ||
                    string.IsNullOrEmpty(request.ShippingInfo.ShipToZip))
                {
                    throw new Exception("One or more required fields are missing.");
                }

                // Get the latitude and longitude using the geocoding service
                var fullAddress = $"{request.ShippingInfo.ShipToAddress}, {request.ShippingInfo.ShipToCity}, {request.ShippingInfo.ShipToState}, {request.ShippingInfo.ShipToZip}";
                var (lat, lng) = await _geocodingService.GetCoordinatesAsync(fullAddress);

                // Create the order object
                var order = new Order
                {
                    UserId = userId,  // Set the main client's ID for the order
                    AdditionalUserId = additionalUserId,  // Set additional user ID if applicable
                    OrderDate = DateTime.Now,
                    OrderStatus = "ordered",
                    ShipToName = request.ShippingInfo.ShipToName,
                    ShipToEmail = request.ShippingInfo.ShipToEmail,
                    ShipToAddress = request.ShippingInfo.ShipToAddress,
                    ShipToAddress2 = request.ShippingInfo.ShipToAddress2,
                    ShipToCity = request.ShippingInfo.ShipToCity,
                    ShipToState = request.ShippingInfo.ShipToState,
                    ShipToZip = request.ShippingInfo.ShipToZip,
                    Lat = lat, // Save the latitude value
                    Lng = lng  // Save the longitude value
                };

                // Add the order to the database
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add each product in the cart as an order detail
                foreach (var item in request.Cart)
                {
                    if (item.product_id == 0 || item.product_id == null)
                    {
                        // Log error or throw exception
                        throw new Exception($"Product ID is missing for one of the cart items: {JsonConvert.SerializeObject(item)}");
                    }

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        product_id = item.product_id, // Ensure the product_id is passed properly
                        Quantity = item.Quantity
                    };

                    _context.OrderDetails.Add(orderDetail);
                }


                // Save order details
                await _context.SaveChangesAsync();

                // Send order confirmation email
                await SendOrderConfirmationEmail(order);

                // Retrieve order confirmation details
                var orderConfirmation = await GetOrderConfirmationAsync(order.OrderId);

                return new JsonResult(new { success = true, orderConfirmation });
            }
            catch (DbUpdateException dbEx)
            {
                // Capture the full details of the database update exception
                var innerException = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                return new JsonResult(new { success = false, message = $"Database error occurred: {innerException}", details = dbEx.ToString() });
            }
            catch (Exception ex)
            {
                // Return detailed error message
                return new JsonResult(new { success = false, message = $"An internal server error occurred: {ex.Message}", details = ex.ToString() });
            }
        }



        // Email confirmation receipt.
        private async Task SendOrderConfirmationEmail(Order order)
        {
            // Fetch the agency registration based on the UserId from the order
            var agencyRegistration = await _context.AgencyRegistrations
                .Include(ar => ar.LnkAgencyClassificationData)
                .FirstOrDefaultAsync(ar => ar.UserId == order.UserId);

            if (agencyRegistration == null)
            {
                throw new Exception("Agency registration not found.");
            }

            // Ensure the registration and classification exist
            var uniqueId = agencyRegistration.LnkAgencyClassificationData.FirstOrDefault()?.UniqueId ?? "N/A";
            var agencyName = agencyRegistration.AgencyName; // Retrieve the Agency Name

            var subject = $"Order Confirmation - Order #{order.OrderId}";

            // Build the order details table dynamically
            var orderDetails = await _context.OrderDetails
                .Where(od => od.OrderId == order.OrderId)
                .ToListAsync();

            var orderItemsHtml = orderDetails
                .Select(od =>
                {
                    var product = _context.Products.FirstOrDefault(p => p.product_id == od.product_id);
                    return $@"
                <tr>
                    <td>{product?.product_description ?? "Unknown Product"}</td>
                    <td>{od.Quantity} {(od.Quantity > 1 ? "orders" : "order")}</td>
                </tr>";
                })
                .Aggregate((current, next) => current + next); // Concatenating the rows

            // Build the email body with dynamic content
            var message = $@"
        <p>Dear Colleague,</p>
        <p>Thank you for successfully submitting your order.</p>
        <p>Upon approval, programs will receive e-notification of all shipping details and products that will be received.</p>
        <p><strong>{agencyName} (#{uniqueId})</strong> has placed an order(s) for the following items:</p>
        <p>{order.ShipToAddress}<br/>
           {order.ShipToAddress2}<br/>
           {order.ShipToCity}, {order.ShipToState} {order.ShipToZip}</p>
        <p><strong>Placed by:</strong> {order.ShipToName}            Order #{order.OrderId}</p>

        <table style='border-collapse: collapse; width: 100%;'>
            <thead>
                <tr>
                    <th style='border: 1px solid black; padding: 8px;'>Product</th>
                    <th style='border: 1px solid black; padding: 8px;'>Quantity</th>
                </tr>
            </thead>
            <tbody>
                {orderItemsHtml}
            </tbody>
        </table>
        <p>If you have any questions about your order, feel free to reply to this email.</p>
        <p>Best regards,<br/>Your Company</p>
    ";

            // Send the email using the IEmailService
            await _emailService.SendEmailAsync(order.ShipToEmail, subject, message);
        }

        public class CheckoutRequest
        {
            public List<CartItem> Cart { get; set; }
            public ShippingInformation ShippingInfo { get; set; }
        }

        public async Task<OrderConfirmation> GetOrderConfirmationAsync(int orderId)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                return null;
            }

            var orderDetailsList = new List<OrderDetailDto>();

            foreach (var od in order.OrderDetails)
            {
                string productDescription = await GetProductDescriptionAsync(od.product_id);

                orderDetailsList.Add(new OrderDetailDto
                {
                    ProductName = !string.IsNullOrEmpty(productDescription) ? productDescription : "Unknown Product",
                    Quantity = od.Quantity,
                    ProductId = od.product_id // Ensure the product_id is passed correctly
                });
            }

            return new OrderConfirmation
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                ShipToName = order.ShipToName,
                ShipToEmail = order.ShipToEmail,
                ShipToAddress = order.ShipToAddress,
                ShipToAddress2 = order.ShipToAddress2,
                ShipToCity = order.ShipToCity,
                ShipToState = order.ShipToState,
                ShipToZip = order.ShipToZip,
                OrderDetails = orderDetailsList
            };
        }

        private async Task<string> GetProductDescriptionAsync(int productId)
        {
            string productDescription = null;
            try
            {
                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT product_description FROM lk_product WHERE product_id = @ProductId";
                        var productIdParam = command.CreateParameter();
                        productIdParam.ParameterName = "@ProductId";
                        productIdParam.Value = productId;
                        command.Parameters.Add(productIdParam);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                productDescription = reader.GetString(0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching product description: {ex.Message}");
            }

            return productDescription;
        }

        public class OrderDetailDto
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public int ProductId { get; set; }

        }

        public class OrderConfirmation
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public string ShipToName { get; set; }
            public string ShipToEmail { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToAddress2 { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
            public List<OrderDetailDto> OrderDetails { get; set; }
        }
    }
}
