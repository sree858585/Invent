using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
namespace WebApplication1.Pages.Client
{
    [Authorize(Roles = "Client,AdditionalUser")]

    public class ShipToSitesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShipToSitesModel> _logger;
        public ShipToSitesModel(ApplicationDbContext context, ILogger<ShipToSitesModel> logger)
        {
            _context = context;
            _logger = logger;
        }
        [BindProperty]
        public List<ShipToSite> ShipToSites { get; set; }
        [BindProperty]
        public int AgencyRegistrationId { get; set; }

        public string RegistrationStatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
               // return Unauthorized("User is not logged in.");
            }

            // Fetch the logged-in user's email
            var userEmail = User.Identity.Name;

            // Check if the logged-in user is an additional user
            var additionalUser = await _context.AdditionalUsers
                .Include(au => au.AgencyRegistration) // Include the AgencyRegistration relation
                .FirstOrDefaultAsync(u => u.Email == userEmail);        

            AgencyRegistration registration = null;

            if (additionalUser != null)
            {
                // If the logged-in user is an additional user, fetch the main user's registration
                registration = await _context.AgencyRegistrations
                    .Include(ar => ar.ShipToSites)
                    .FirstOrDefaultAsync(ar => ar.Id == additionalUser.AgencyRegistrationId);
            }
            else
            {
                // If the logged-in user is the main user, fetch their own registration
                registration = await _context.AgencyRegistrations
                    .Include(ar => ar.ShipToSites)
                    .FirstOrDefaultAsync(ar => ar.UserId == userId);
            }

            if (registration == null)
            {
                // Set a user-friendly message when registration is not found
                RegistrationStatusMessage = "You are not a registered user yet.";
                ShipToSites = new List<ShipToSite>(); // Clear the list to avoid null reference issues
                return Page();
            }

            AgencyRegistrationId = registration.Id;
            ShipToSites = registration.ShipToSites.ToList();

            return Page();
        }


        public async Task<IActionResult> OnGetShipToSiteDetailsAsync(int id)
        {
            try
            {
                var shipToSite = await _context.ShipToSites
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Address,
                    s.Address2,
                    s.City,
                    s.State,
                    s.Zip,
                    s.Phone,
                    s.PhoneExtension,
                    s.ShipToEmail,
                    s.SiteType,
                    s.ShipToName,
                    s.ShipToAddress,
                    s.ShipToAddress2,
                    s.ShipToCity,
                    s.ShipToState,
                    s.ShipToZip
                })
                .FirstOrDefaultAsync();
                if (shipToSite == null)
                {
                    return new JsonResult(new { success = false, message = "Ship to site not found." });
                }
                return new JsonResult(new { success = true, shipToSite });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ship to site details for ID {Id}", id);
                return new JsonResult(new { success = false, message = "Error fetching ship to site details." });
            }
        }


        public async Task<IActionResult> OnPostSaveShipToSiteAsync([FromBody] ShipToSite site)
        {
            if (site == null || site.AgencyRegistrationId == 0)
            {
                // Try to set AgencyRegistrationId manually if it's missing
                site.AgencyRegistrationId = AgencyRegistrationId;
                if (site.AgencyRegistrationId == 0)
                {
                    _logger.LogError("AgencyRegistrationId is required.");
                    return new JsonResult(new { success = false, message = "AgencyRegistrationId is required." });
                }
            }

            if (!ModelState.IsValid)
            {
                //return new JsonResult(new { success = false, message = "Invalid data provided." });
            }

            try
            {
                if (site.Id == 0)
                {
                    // Add new ShipToSite
                    _context.ShipToSites.Add(site);
                }
                else
                {
                    // Update existing ShipToSite
                    var existingSite = await _context.ShipToSites.FindAsync(site.Id);
                    if (existingSite == null)
                    {
                        return new JsonResult(new { success = false, message = "Ship to site not found." });
                    }

                    // Map all properties from the incoming site to the existing site
                    existingSite.Name = site.Name;
                    existingSite.Address = site.Address;
                    existingSite.Address2 = site.Address2;
                    existingSite.City = site.City;
                    existingSite.State = site.State;
                    existingSite.Zip = site.Zip;
                    existingSite.Phone = site.Phone;
                    existingSite.PhoneExtension = site.PhoneExtension;
                    existingSite.ShipToEmail = site.ShipToEmail;
                    existingSite.SiteType = site.SiteType;
                    existingSite.ShipToName = site.ShipToName;
                    existingSite.ShipToAddress = site.ShipToAddress;
                    existingSite.ShipToAddress2 = site.ShipToAddress2;
                    existingSite.ShipToCity = site.ShipToCity;
                    existingSite.ShipToState = site.ShipToState;
                    existingSite.ShipToZip = site.ShipToZip;
                    existingSite.SameAsSite = site.SameAsSite;
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving ship to site.");
                return new JsonResult(new { success = false, message = "Error saving ship to site." });
            }
        }

    }
}