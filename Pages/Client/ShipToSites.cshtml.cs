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

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                //return Unauthorized("User is not logged in.");
            }

            var registration = await _context.AgencyRegistrations
                .Include(ar => ar.ShipToSites)
                .FirstOrDefaultAsync(ar => ar.UserId == userId);

            if (registration == null)
            {
                return NotFound("Registration not found.");
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                       .Select(x => new { x.Key, x.Value.Errors })
                                       .ToArray();
                foreach (var error in errors)
                {
                    foreach (var err in error.Errors)
                    {
                        _logger.LogError($"Validation Error in '{error.Key}': {err.ErrorMessage}");
                    }
                }

                return new JsonResult(new { success = false, message = "Invalid data provided." });
            }

            if (site.Id == 0)
            {
                site.AgencyRegistrationId = AgencyRegistrationId; // Set the correct registration ID
                _context.ShipToSites.Add(site);
            }
            else
            {
                var existingSite = await _context.ShipToSites.FindAsync(site.Id);
                if (existingSite == null)
                {
                    return new JsonResult(new { success = false, message = "Ship to site not found." });
                }

                // Map all properties from incoming site to existing site
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

            try
            {
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
