using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
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
    public class PlaceOrderModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; // Inject EmailService

        public PlaceOrderModel(ApplicationDbContext context, IEmailService emailService) // Inject IEmailService here
        {
            _context = context;
            _emailService = emailService; // Assign the email service
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
                System.Diagnostics.Debug.WriteLine("OnPostCheckoutAsync method called");

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

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                        product_id = item.product_id,
                        Quantity = item.Quantity
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                await _context.SaveChangesAsync();

                // Prepare and send the email confirmation after successful order placement
                await SendOrderConfirmationEmail(order);


                var orderConfirmation = await GetOrderConfirmationAsync(order.OrderId);

                return new JsonResult(new { success = true, orderConfirmation });
            }
            catch (Exception ex)
            {
                // Log the exception for further analysis
                System.Diagnostics.Debug.WriteLine("An error occurred: " + ex.Message);
                return new JsonResult(new { success = false, message = "An internal server error occurred." });
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
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

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
                    Quantity = od.Quantity
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
                // Log the exception
                Console.WriteLine($"Error fetching product description: {ex.Message}");
            }

            return productDescription;
        }





        public class OrderDetailDto
        {
            public string ProductName { get; set; }
            public int Quantity { get; set; }
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
