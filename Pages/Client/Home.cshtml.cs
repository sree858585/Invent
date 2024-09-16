using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using System;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Pages.Client
{
    [Authorize(Roles = "Client,AdditionalUser")]
    public class HomeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public HomeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public OrderViewModel LatestOrder { get; set; }
        public RegistrationViewModel LatestRegistration { get; set; }
        public ReportViewModel LatestReport { get; set; }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentYear = DateTime.Now.Year;

            // Get the Client's User ID (for AdditionalUser, we need the Client's UserId)
            var clientUserId = await GetClientUserIdIfNeeded(userId);

            // 1. Retrieve Latest Order - Different for Client and AdditionalUser
            if (User.IsInRole("Client"))
            {
                // If Client, fetch their own orders
                LatestOrder = await _context.Orders
                    .Where(o => o.UserId == userId) // Only fetch orders of the Client
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new OrderViewModel
                    {
                        OrderId = o.OrderId,
                        OrderDate = o.OrderDate,
                        OrderStatus = o.OrderStatus,
                        ShipToName = o.ShipToName,
                        ShipToAddress = o.ShipToAddress,
                        ShipToCity = o.ShipToCity,
                        ShipToState = o.ShipToState,
                        ShipToZip = o.ShipToZip,
                        ApprovedDate = o.ApprovedDate,
                        CanceledDate = o.CanceledDate,
                        ShippedDate = o.ShippedDate
                    })
                    .FirstOrDefaultAsync();
            }
            else if (User.IsInRole("AdditionalUser"))
            {
                // Fetch the Client's UserId to whom this AdditionalUser belongs
                var clientUserId1 = await _context.AdditionalUsers
                    .Where(au => au.Email == User.Identity.Name) // Assuming email identifies the AdditionalUser
                    .Select(au => au.AgencyRegistration.UserId)   // Fetch the Client's UserId
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(clientUserId1))
                {
                    // Fetch the orders tied to the Client
                    LatestOrder = await _context.Orders
                        .Where(o => o.UserId == clientUserId1 || o.UserId == userId) // Fetch Client's or AdditionalUser's orders
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => new OrderViewModel
                        {
                            OrderId = o.OrderId,
                            OrderDate = o.OrderDate,
                            OrderStatus = o.OrderStatus,
                            ShipToName = o.ShipToName,
                            ShipToAddress = o.ShipToAddress,
                            ShipToCity = o.ShipToCity,
                            ShipToState = o.ShipToState,
                            ShipToZip = o.ShipToZip,
                            ApprovedDate = o.ApprovedDate,
                            CanceledDate = o.CanceledDate,
                            ShippedDate = o.ShippedDate
                        })
                        .FirstOrDefaultAsync();
                }
                else
                {
                    // If AdditionalUser has no Client, fetch their own orders
                    LatestOrder = await _context.Orders
                        .Where(o => o.UserId == userId) // Fetch AdditionalUser's orders only
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => new OrderViewModel
                        {
                            OrderId = o.OrderId,
                            OrderDate = o.OrderDate,
                            OrderStatus = o.OrderStatus,
                            ShipToName = o.ShipToName,
                            ShipToAddress = o.ShipToAddress,
                            ShipToCity = o.ShipToCity,
                            ShipToState = o.ShipToState,
                            ShipToZip = o.ShipToZip,
                            ApprovedDate = o.ApprovedDate,
                            CanceledDate = o.CanceledDate,
                            ShippedDate = o.ShippedDate
                        })
                        .FirstOrDefaultAsync();
                }
            }

            // 2. Retrieve Registration (Common for both Client and AdditionalUser)
            LatestRegistration = await _context.AgencyRegistrations
                .Where(r => r.UserId == clientUserId) // Use the Client's UserId
                .OrderByDescending(r => r.SubmissionDate)
                .Select(r => new RegistrationViewModel
                {
                    AgencyName = r.AgencyName,
                    RegistrationDate = r.SubmissionDate,
                    Status = r.Status,
                    RegistrationType = r.RegistrationType
                })
                .FirstOrDefaultAsync();

            // 3. Retrieve Quarterly Report - Separate for Client and AdditionalUser
            string latestQuarter;
            DateTime dueDate;
            (latestQuarter, dueDate) = GetLatestQuarter(currentYear);

            if (User.IsInRole("Client"))
            {
                // Fetch reports for the Client
                LatestReport = await _context.QuarterlyReports
                    .Where(r => r.UserId == userId && r.Year == currentYear && r.Quarter == latestQuarter)
                    .Select(r => new ReportViewModel
                    {
                        Year = r.Year ?? currentYear,
                        QuarterName = r.Quarter,
                        DueDate = r.DueDate ?? DateTime.MinValue,
                        Status = r.Status
                    })
                    .FirstOrDefaultAsync();
            }
            else if (User.IsInRole("AdditionalUser"))
            {
                // Fetch reports for the AdditionalUser
                LatestReport = await _context.QuarterlyReports
                    .Where(r => r.UserId == userId && r.Year == currentYear && r.Quarter == latestQuarter) // Fetch only AdditionalUser's reports
                    .Select(r => new ReportViewModel
                    {
                        Year = r.Year ?? currentYear,
                        QuarterName = r.Quarter,
                        DueDate = r.DueDate ?? DateTime.MinValue,
                        Status = r.Status
                    })
                    .FirstOrDefaultAsync();
            }

            if (LatestReport == null)
            {
                LatestReport = new ReportViewModel
                {
                    Year = currentYear,
                    QuarterName = latestQuarter,
                    DueDate = dueDate,
                    Status = "Pending"
                };
            }
        }

        // Method to get the Client's User ID if the current user is an AdditionalUser
        private async Task<string> GetClientUserIdIfNeeded(string userId)
        {
            // If the current user is an AdditionalUser, fetch the Client's UserId
            var clientUserId = await _context.AdditionalUsers
                .Where(au => au.Email == User.Identity.Name) // Assuming email identifies the user
                .Select(au => au.AgencyRegistration.UserId)   // Fetch the Client's UserId
                .FirstOrDefaultAsync();

            // If the user is not an AdditionalUser, return their own UserId (i.e., they are a Client)
            return clientUserId ?? userId;
        }

        // Method to determine the latest quarter based on the current date
        private (string, DateTime) GetLatestQuarter(int currentYear)
        {
            string latestQuarter;
            DateTime dueDate;
            if (DateTime.Now.Month >= 1 && DateTime.Now.Month <= 3)
            {
                latestQuarter = "Q1";
                dueDate = new DateTime(currentYear, 4, 15);
            }
            else if (DateTime.Now.Month >= 4 && DateTime.Now.Month <= 6)
            {
                latestQuarter = "Q2";
                dueDate = new DateTime(currentYear, 7, 15);
            }
            else if (DateTime.Now.Month >= 7 && DateTime.Now.Month <= 9)
            {
                latestQuarter = "Q3";
                dueDate = new DateTime(currentYear, 10, 15);
            }
            else
            {
                latestQuarter = "Q4";
                dueDate = new DateTime(currentYear + 1, 1, 15);
            }
            return (latestQuarter, dueDate);
        }

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string OrderStatus { get; set; }
            public string ShipToName { get; set; }
            public string ShipToAddress { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToState { get; set; }
            public string ShipToZip { get; set; }
            public DateTime OrderDate { get; set; }
            public DateTime? ApprovedDate { get; set; }
            public DateTime? CanceledDate { get; set; }
            public DateTime? ShippedDate { get; set; }
        }

        public class RegistrationViewModel
        {
            public string AgencyName { get; set; }
            public DateTime RegistrationDate { get; set; }
            public string Status { get; set; }
            public string RegistrationType { get; set; }
        }

        public class ReportViewModel
        {
            public string QuarterName { get; set; }
            public int Year { get; set; }
            public DateTime DueDate { get; set; }
            public string Status { get; set; }
        }
    }
}
