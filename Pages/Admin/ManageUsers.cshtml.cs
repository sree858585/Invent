using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageUsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManageUsersModel> _logger;

        public ManageUsersModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context, ILogger<ManageUsersModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Email { get; set; }

        [BindProperty(SupportsGet = true)]
        public string AgencyName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string UniqueId { get; set; }

        public ApplicationUser User { get; set; }

        public List<AgencyRegistration> Registrations { get; set; }

        [BindProperty]
        public AgencyRegistration Registration { get; set; }

        [BindProperty]
        public AgencyContact AgencyContact { get; set; }

        [BindProperty]
        public List<AdditionalUser> AdditionalUsers { get; set; }

        [BindProperty]
        public AdditionalUser AdditionalUserToEdit { get; set; }

        [BindProperty]
        public ShipInformation ShipInformation { get; set; }

        [BindProperty]
        public List<ShipToSite> AdditionalShipToSites { get; set; }

        [BindProperty]
        public ShipToSite ShipToSiteToEdit { get; set; }

        [BindProperty]
        public List<int> CountiesServed { get; set; } = new List<int>(new int[10]);

        public List<SelectListItem> CountyList { get; set; }

        public List<LnkAgencyClassificationData> LnkAgencyClassificationDataList { get; set; }  // Updated line

        public string SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Start with a base query that includes related entities
            var query = _context.AgencyRegistrations
                .Include(r => r.AgencyContacts)
                .Include(r => r.LnkAgencyClassificationData)
                .AsQueryable();

            // Apply filters based on the search criteria
            if (!string.IsNullOrEmpty(Email))
            {
                query = query.Where(r => r.AgencyContacts.Any(c => c.Email == Email));
            }

            if (!string.IsNullOrEmpty(AgencyName))
            {
                query = query.Where(r => r.AgencyName.Contains(AgencyName));
            }

            if (!string.IsNullOrEmpty(UniqueId))
            {
                query = query.Where(r => r.LnkAgencyClassificationData.Any(d => d.UniqueId == UniqueId));
            }

            // Fetch the filtered results
            Registrations = await query.ToListAsync();

            // Populate the CountyList for dropdowns, etc.
            CountyList = await _context.Counties
                .Where(c => c.is_active)
                .Select(c => new SelectListItem { Value = c.county_id.ToString(), Text = c.name })
                .ToListAsync();

            // Ensure the CountiesServed list has a minimum number of items for display
            while (CountiesServed.Count < 10)
            {
                CountiesServed.Add(0);
            }
        }



        public async Task<IActionResult> OnPostEditAsync(int registrationId)
        {
            _logger.LogInformation($"OnPostEditAsync called with RegistrationId: {registrationId}");
            Registration = await _context.AgencyRegistrations
                .FirstOrDefaultAsync(r => r.Id == registrationId);
            if (Registration == null)
            {
                _logger.LogWarning($"Registration with Id: {registrationId} not found.");
                return NotFound();
            }

            AgencyContact = await _context.AgencyContacts
                .FirstOrDefaultAsync(ac => ac.AgencyRegistrationId == registrationId);
            AdditionalUsers = await _context.AdditionalUsers
                .Where(au => au.AgencyRegistrationId == registrationId).ToListAsync();
            ShipInformation = await _context.ShipInformations
                .FirstOrDefaultAsync(si => si.AgencyRegistrationId == registrationId);
            AdditionalShipToSites = await _context.ShipToSites
                .Where(sts => sts.AgencyRegistrationId == registrationId).ToListAsync();

            CountiesServed = await _context.EaspSepsRegistrationCounties
                .Where(c => c.AgencyRegistrationId == registrationId)
                .Select(c => c.CountyId)
                .Take(10)
                .ToListAsync();

            while (CountiesServed.Count < 10)
            {
                CountiesServed.Add(0);
            }

            CountyList = await _context.Counties
                .Where(c => c.is_active)
                .Select(c => new SelectListItem { Value = c.county_id.ToString(), Text = c.name })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateEaspSepsRegistrationAsync()
        {
            _logger.LogInformation("OnPostUpdateEaspSepsRegistrationAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
            }

            var registration = await _context.AgencyRegistrations
                .FirstOrDefaultAsync(r => r.Id == Registration.Id);

            if (registration != null)
            {
                registration.AgencyName = Registration.AgencyName;
                registration.AlternateName = Registration.AlternateName;
                registration.County = Registration.County;
                registration.Address = Registration.Address;
                registration.Address2 = Registration.Address2;
                registration.City = Registration.City;
                registration.State = Registration.State;
                registration.Zip = Registration.Zip;
                registration.RegistrationType = Registration.RegistrationType;

                _context.AgencyRegistrations.Update(registration);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "EaspSepsRegistration updated successfully.";
            }
            else
            {
                _logger.LogWarning($"EaspSepsRegistration with Id: {Registration.Id} not found.");
            }

            User = await _userManager.FindByIdAsync(registration.UserId);
            var email = User != null ? User.Email : Email;
            if (User == null)
            {
                _logger.LogWarning($"User with Id: {registration.UserId} not found.");
            }

            return RedirectToPage("/Admin/ManageUsers");
        }

        public async Task<IActionResult> OnPostUpdateAgencyContactAsync()
        {
            _logger.LogInformation("OnPostUpdateAgencyContactAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                //  return Page(); // Return the page to show validation errors
            }

            // Fetch the AgencyRegistration by ID
            var registration = await _context.AgencyRegistrations
                .FirstOrDefaultAsync(r => r.Id == Registration.Id);

            // Check if the registration is null
            if (registration == null)
            {
                _logger.LogWarning($"AgencyRegistration with Id {Registration.Id} not found.");
                ModelState.AddModelError(string.Empty, "Invalid registration ID. The registration does not exist.");
                //return Page();
            }

            // Update the AgencyContact if it exists
            var agencyContact = await _context.AgencyContacts
                .FirstOrDefaultAsync(ac => ac.AgencyRegistrationId == registration.Id);

            if (agencyContact != null)
            {
                agencyContact.ProgramDirector = AgencyContact.ProgramDirector;
                agencyContact.SuffixId = AgencyContact.SuffixId;
                agencyContact.Address = AgencyContact.Address;
                agencyContact.Address2 = AgencyContact.Address2;
                agencyContact.City = AgencyContact.City;
                agencyContact.State = AgencyContact.State;
                agencyContact.Zip = AgencyContact.Zip;

                _context.AgencyContacts.Update(agencyContact);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "AgencyContact updated successfully.";
            }
            else
            {
                _logger.LogWarning($"AgencyContact for RegistrationId: {registration.Id} not found.");
                ModelState.AddModelError(string.Empty, "Agency contact not found.");
                return Page();
            }

            // Fetch the user associated with this registration, but check if UserId exists first
            if (!string.IsNullOrEmpty(registration.UserId))
            {
                User = await _userManager.FindByIdAsync(registration.UserId);
                if (User == null)
                {
                    _logger.LogWarning($"User with Id: {registration.UserId} not found.");
                    return NotFound();
                }
                var email = User.Email ?? Email; // Fallback to the provided email if User.Email is null
                return RedirectToPage("/Admin/ManageUsers");
            }
            else
            {
                _logger.LogWarning($"Registration with Id: {registration.Id} does not have an associated UserId.");
                ModelState.AddModelError(string.Empty, "No user associated with this registration.");
                return Page();
            }
        }


        public async Task<IActionResult> OnPostUpdateAdditionalUserAsync()
        {
            _logger.LogInformation("OnPostUpdateAdditionalUserAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
            }

            var additionalUser = await _context.AdditionalUsers
                .FirstOrDefaultAsync(au => au.Id == AdditionalUserToEdit.Id);

            if (additionalUser != null)
            {
                additionalUser.Name = AdditionalUserToEdit.Name;
                additionalUser.Title = AdditionalUserToEdit.Title;
                additionalUser.Address = AdditionalUserToEdit.Address;
                additionalUser.Address2 = AdditionalUserToEdit.Address2;
                additionalUser.City = AdditionalUserToEdit.City;
                additionalUser.State = AdditionalUserToEdit.State;
                additionalUser.Zip = AdditionalUserToEdit.Zip;
                additionalUser.Phone = AdditionalUserToEdit.Phone;
                additionalUser.Email = AdditionalUserToEdit.Email;

                _context.AdditionalUsers.Update(additionalUser);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "AdditionalUser updated successfully.";
            }
            else
            {
                _logger.LogWarning($"AdditionalUser with Id: {AdditionalUserToEdit.Id} not found.");
                return NotFound();
            }

            var registration = await _context.AgencyRegistrations
                .FirstOrDefaultAsync(r => r.Id == additionalUser.AgencyRegistrationId);

            if (registration != null)
            {
                User = await _userManager.FindByIdAsync(registration.UserId.ToString());
                var email = User != null ? User.Email : Email;
                return RedirectToPage("/Admin/ManageUsers");
            }
            else
            {
                _logger.LogWarning($"Registration with Id: {additionalUser.AgencyRegistrationId} not found.");
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostUpdateShippingInfoAsync()
        {
            _logger.LogInformation("OnPostUpdateShippingInfoAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                // return Page(); // Return to the page to display validation errors
            }

            // Check if the AgencyRegistrationId exists in the AgencyRegistrations table
            var registration = await _context.AgencyRegistrations
                .FirstOrDefaultAsync(r => r.Id == Registration.Id);

            if (registration == null)
            {
                _logger.LogWarning($"AgencyRegistration with Id {Registration.Id} not found.");
                ModelState.AddModelError(string.Empty, "Invalid registration ID. The registration does not exist.");
                // return Page();
            }

            var shipInformation = await _context.ShipInformations
                .FirstOrDefaultAsync(si => si.AgencyRegistrationId == Registration.Id);

            // If ShipInformation is null, create a new entry
            if (shipInformation == null)
            {
                shipInformation = new ShipInformation
                {
                    AgencyRegistrationId = Registration.Id, // Ensure this is a valid foreign key
                    ShipToName = ShipInformation.ShipToName,
                    ShipToEmail = ShipInformation.ShipToEmail,
                    ShipToAddress = ShipInformation.ShipToAddress,
                    ShipToAddress2 = ShipInformation.ShipToAddress2,
                    ShipToCity = ShipInformation.ShipToCity,
                    ShipToState = ShipInformation.ShipToState,
                    ShipToZip = ShipInformation.ShipToZip
                };
                _context.ShipInformations.Add(shipInformation);
                _logger.LogInformation("New ShipInformation created.");
            }
            else
            {
                // Update the existing ShipInformation
                shipInformation.ShipToName = ShipInformation.ShipToName;
                shipInformation.ShipToEmail = ShipInformation.ShipToEmail;
                shipInformation.ShipToAddress = ShipInformation.ShipToAddress;
                shipInformation.ShipToAddress2 = ShipInformation.ShipToAddress2;
                shipInformation.ShipToCity = ShipInformation.ShipToCity;
                shipInformation.ShipToState = ShipInformation.ShipToState;
                shipInformation.ShipToZip = ShipInformation.ShipToZip;
                _context.ShipInformations.Update(shipInformation);
                _logger.LogInformation("Existing ShipInformation updated.");
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "ShipInformation updated successfully.";

            return RedirectToPage("/Admin/ManageUsers");
        }






        public async Task<IActionResult> OnPostUpdateShipToSiteAsync()
        {
            _logger.LogInformation("OnPostUpdateShipToSiteAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
            }

            var shipToSite = await _context.ShipToSites
                .FirstOrDefaultAsync(sts => sts.Id == ShipToSiteToEdit.Id);

            if (shipToSite != null)
            {
                shipToSite.Name = ShipToSiteToEdit.Name;
                shipToSite.Address = ShipToSiteToEdit.Address;
                shipToSite.Address2 = ShipToSiteToEdit.Address2;
                shipToSite.City = ShipToSiteToEdit.City;
                shipToSite.State = ShipToSiteToEdit.State;
                shipToSite.Zip = ShipToSiteToEdit.Zip;
                shipToSite.Phone = ShipToSiteToEdit.Phone;
                shipToSite.PhoneExtension = ShipToSiteToEdit.PhoneExtension;
                shipToSite.SiteType = ShipToSiteToEdit.SiteType;
                shipToSite.ShipToName = ShipToSiteToEdit.ShipToName;
                shipToSite.ShipToEmail = ShipToSiteToEdit.ShipToEmail;
                shipToSite.ShipToAddress = ShipToSiteToEdit.ShipToAddress;
                shipToSite.ShipToAddress2 = ShipToSiteToEdit.ShipToAddress2;
                shipToSite.ShipToCity = ShipToSiteToEdit.ShipToCity;
                shipToSite.ShipToState = ShipToSiteToEdit.ShipToState;
                shipToSite.ShipToZip = ShipToSiteToEdit.ShipToZip;
                shipToSite.SameAsSite = ShipToSiteToEdit.SameAsSite;

                _context.ShipToSites.Update(shipToSite);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "ShipToSite updated successfully.";
            }
            else
            {
                _logger.LogWarning($"ShipToSite with Id: {ShipToSiteToEdit.Id} not found.");
                return NotFound();
            }

            var registration = await _context.AgencyRegistrations
                .FirstOrDefaultAsync(r => r.Id == shipToSite.AgencyRegistrationId);

            if (registration != null)
            {
                User = await _userManager.FindByIdAsync(registration.UserId.ToString());
                var email = User != null ? User.Email : Email;
                return RedirectToPage("/Admin/ManageUsers");
            }
            else
            {
                _logger.LogWarning($"Registration with Id: {shipToSite.AgencyRegistrationId} not found.");
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostUpdateCountiesServedAsync()
        {
            _logger.LogInformation("OnPostUpdateCountiesServedAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
            }

            if (Registration == null)
            {
                _logger.LogWarning("Registration object is null.");
                return BadRequest("Registration data is missing or invalid.");
            }

            // Proceed with updating the counties served
            var existingCounties = await _context.EaspSepsRegistrationCounties
                .Where(c => c.AgencyRegistrationId == Registration.Id)
                .ToListAsync();

            foreach (var existingCounty in existingCounties)
            {
                if (!CountiesServed.Contains(existingCounty.CountyId))
                {
                    _context.EaspSepsRegistrationCounties.Remove(existingCounty);
                }
            }

            foreach (var countyId in CountiesServed)
            {
                if (countyId > 0 && !existingCounties.Any(c => c.CountyId == countyId))
                {
                    var newCounty = new EaspSepsRegistrationCounty
                    {
                        AgencyRegistrationId = Registration.Id,
                        CountyId = countyId
                    };

                    var existingEntity = _context.ChangeTracker.Entries<EaspSepsRegistrationCounty>()
                        .FirstOrDefault(e => e.Entity.AgencyRegistrationId == Registration.Id && e.Entity.CountyId == countyId);

                    if (existingEntity == null)
                    {
                        _context.EaspSepsRegistrationCounties.Add(newCounty);
                    }
                    else
                    {
                        _context.Entry(existingEntity.Entity).State = EntityState.Modified;
                    }
                }
            }

            await _context.SaveChangesAsync();


            //// Check if Registration.UserId is null
            //if (!string.IsNullOrEmpty(Registration.UserId))
            //{
            //    var user = await _userManager.FindByIdAsync(Registration.UserId.ToString());
            //    var email = user != null ? user.Email : Email;
            //    return RedirectToPage("/Admin/ManageUsers", new { email = email });
            //}

            TempData["SuccessMessage"] = "CountiesServed updated successfully."; // Set success message

            return RedirectToPage("/Admin/ManageUsers");

        }




        public async Task<IActionResult> OnGetAdditionalUserAsync(int id)
        {
            var additionalUser = await _context.AdditionalUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (additionalUser == null)
            {
                return NotFound();
            }
            return new JsonResult(additionalUser);
        }

        public async Task<IActionResult> OnGetShipToSiteAsync(int id)
        {
            var shipToSite = await _context.ShipToSites.FirstOrDefaultAsync(s => s.Id == id);
            if (shipToSite == null)
            {
                return NotFound();
            }
            return new JsonResult(shipToSite);
        }
    }
}