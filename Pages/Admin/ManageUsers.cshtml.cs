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
        public string Email
        {
            get; set;
        }
        public ApplicationUser User { get; set; }

        public List<EaspSepsRegistration> Registrations { get; set; }

        [BindProperty]
        public EaspSepsRegistration Registration { get; set; }

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

        public string SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            _logger.LogInformation($"OnGetAsync called with Email: {Email}");

            if (!string.IsNullOrEmpty(Email))
            {
                User = await _userManager.FindByEmailAsync(Email);
                if (User != null)
                {
                    Registrations = await _context.EaspSepsRegistrations
                        .Where(r => r.UserId == User.Id)
                        .ToListAsync();
                }
            }

            CountyList = await _context.Counties
                .Where(c => c.is_active)
                .Select(c => new SelectListItem { Value = c.county_id.ToString(), Text = c.name })
                .ToListAsync();

            // Initialize CountiesServed with 5 elements to avoid IndexOutOfRangeException
            while (CountiesServed.Count < 5)
            {
                CountiesServed.Add(0);
            }
        }

        public async Task<IActionResult> OnPostEditAsync(int registrationId)
        {
            _logger.LogInformation($"OnPostEditAsync called with RegistrationId: {registrationId}");
            Registration = await _context.EaspSepsRegistrations
                .FirstOrDefaultAsync(r => r.Id == registrationId);
            if (Registration == null)
            {
                _logger.LogWarning($"Registration with Id: {registrationId} not found.");
                return NotFound();
            }

            // Load related data
            AgencyContact = await _context.AgencyContacts
                .FirstOrDefaultAsync(ac => ac.EaspSepsRegistrationId == registrationId);
            AdditionalUsers = await _context.AdditionalUsers
                .Where(au => au.EaspSepsRegistrationId == registrationId).ToListAsync();
            ShipInformation = await _context.ShipInformations
                .FirstOrDefaultAsync(si => si.EaspSepsRegistrationId == registrationId);
            AdditionalShipToSites = await _context.ShipToSites
                .Where(sts => sts.EaspSepsRegistrationId == registrationId).ToListAsync();

            CountiesServed = await _context.EaspSepsRegistrationCounties
                .Where(c => c.EaspSepsRegistrationId == registrationId)
                .Select(c => c.CountyId)
                .Take(10)
                .ToListAsync();

            // Ensure CountiesServed has at least 10 elements
            while (CountiesServed.Count < 10)
            {
                CountiesServed.Add(0); // 0 or some default value indicating no selection
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
              //  return Page();
            }

            var registration = await _context.EaspSepsRegistrations
                .FirstOrDefaultAsync(r => r.Id == Registration.Id);

            if (registration != null)
            {
                _logger.LogInformation($"Updating EaspSepsRegistration with Id: {registration.Id}");
                registration.AgencyName = Registration.AgencyName;
                registration.AlternateName = Registration.AlternateName;
                registration.County = Registration.County;
                registration.UniqueId = Registration.UniqueId;
                registration.Address = Registration.Address;
                registration.Address2 = Registration.Address2;
                registration.City = Registration.City;
                registration.State = Registration.State;
                registration.Zip = Registration.Zip;
                registration.RegistrationType = Registration.RegistrationType;

                _context.EaspSepsRegistrations.Update(registration);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated EaspSepsRegistration with Id: {registration.Id}");
                SuccessMessage = "EaspSepsRegistration updated successfully.";
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

            return RedirectToPage("/Admin/ManageUsers", new { email = email });
        }

        public async Task<IActionResult> OnPostUpdateAgencyContactAsync()
        {
            _logger.LogInformation("OnPostUpdateAgencyContactAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                //return Page();
            }

            var agencyContact = await _context.AgencyContacts
                .FirstOrDefaultAsync(ac => ac.EaspSepsRegistrationId == Registration.Id);

            if (agencyContact != null)
            {
                // Update agency contact fields
                agencyContact.ProgramDirector = AgencyContact.ProgramDirector;
                agencyContact.SuffixId = AgencyContact.SuffixId;
                agencyContact.Address = AgencyContact.Address;
                agencyContact.Address2 = AgencyContact.Address2;
                agencyContact.City = AgencyContact.City;
                agencyContact.State = AgencyContact.State;
                agencyContact.Zip = AgencyContact.Zip;

                _context.AgencyContacts.Update(agencyContact);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated AgencyContact with Id: {agencyContact.Id}");
                SuccessMessage = "AgencyContact updated successfully.";
            }
            else
            {
                _logger.LogWarning($"AgencyContact for RegistrationId: {Registration.Id} not found.");
            }

            var registration = await _context.EaspSepsRegistrations
                .FirstOrDefaultAsync(r => r.Id == Registration.Id);

            User = await _userManager.FindByIdAsync(registration.UserId);
            var email = User != null ? User.Email : Email;
            if (User == null)
            {
                _logger.LogWarning($"User with Id: {registration.UserId} not found.");
            }

            return RedirectToPage("/Admin/ManageUsers", new { email = email });
        }

        public async Task<IActionResult> OnPostUpdateAdditionalUserAsync()
        {
            _logger.LogInformation("OnPostUpdateAdditionalUserAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
               // return Page();
            }

            var additionalUser = await _context.AdditionalUsers
                .FirstOrDefaultAsync(au => au.Id == AdditionalUserToEdit.Id);

            if (additionalUser != null)
            {
                // Update fields
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
                _logger.LogInformation($"Updated AdditionalUser with Id: {additionalUser.Id}");
                SuccessMessage = "AdditionalUser updated successfully.";
            }
            else
            {
                _logger.LogWarning($"AdditionalUser with Id: {AdditionalUserToEdit.Id} not found.");
                return NotFound();
            }

            // Retrieve the registration object again to get the UserId
            var registration = await _context.EaspSepsRegistrations
                .FirstOrDefaultAsync(r => r.Id == additionalUser.EaspSepsRegistrationId);

            if (registration != null)
            {
                User = await _userManager.FindByIdAsync(registration.UserId.ToString());
                var email = User != null ? User.Email : Email;
                return RedirectToPage("/Admin/ManageUsers", new { email = email });
            }
            else
            {
                _logger.LogWarning($"Registration with Id: {additionalUser.EaspSepsRegistrationId} not found.");
                return NotFound();
            }
        }


        public async Task<IActionResult> OnPostUpdateShippingInfoAsync()
        {
            _logger.LogInformation("OnPostUpdateShippingInfoAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
               // return Page();
            }

            var shipInformation = await _context.ShipInformations
                .FirstOrDefaultAsync(si => si.EaspSepsRegistrationId == Registration.Id);

            if (shipInformation != null)
            {
                // Update fields
                shipInformation.ShipToName = ShipInformation.ShipToName;
                shipInformation.ShipToEmail = ShipInformation.ShipToEmail;
                shipInformation.ShipToAddress = ShipInformation.ShipToAddress;
                shipInformation.ShipToAddress2 = ShipInformation.ShipToAddress2;
                shipInformation.ShipToCity = ShipInformation.ShipToCity;
                shipInformation.ShipToState = ShipInformation.ShipToState;
                shipInformation.ShipToZip = ShipInformation.ShipToZip;

                _context.ShipInformations.Update(shipInformation);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated ShipInformation with Id: {shipInformation.Id}");
                SuccessMessage = "ShipInformation updated successfully.";
            }
            else
            {
                _logger.LogWarning($"ShipInformation for RegistrationId: {Registration.Id} not found.");
            }

            var registration = await _context.EaspSepsRegistrations
                .FirstOrDefaultAsync(r => r.Id == Registration.Id);

            User = await _userManager.FindByIdAsync(registration.UserId);
            var email = User != null ? User.Email : Email;
            if (User == null)
            {
                _logger.LogWarning($"User with Id: {registration.UserId} not found.");
            }

            return RedirectToPage("/Admin/ManageUsers", new { email = email });
        }

        public async Task<IActionResult> OnPostUpdateShipToSiteAsync()
        {
            _logger.LogInformation("OnPostUpdateShipToSiteAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                //return Page();
            }

            var shipToSite = await _context.ShipToSites
                .FirstOrDefaultAsync(sts => sts.Id == ShipToSiteToEdit.Id);

            if (shipToSite != null)
            {
                // Update fields
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
                _logger.LogInformation($"Updated ShipToSite with Id: {shipToSite.Id}");
                SuccessMessage = "ShipToSite updated successfully.";
            }
            else
            {
                _logger.LogWarning($"ShipToSite with Id: {ShipToSiteToEdit.Id} not found.");
                return NotFound();
            }

            // Retrieve the registration object again to get the UserId
            var registration = await _context.EaspSepsRegistrations
                .FirstOrDefaultAsync(r => r.Id == shipToSite.EaspSepsRegistrationId);

            if (registration != null)
            {
                User = await _userManager.FindByIdAsync(registration.UserId.ToString());
                var email = User != null ? User.Email : Email;
                return RedirectToPage("/Admin/ManageUsers", new { email = email });
            }
            else
            {
                _logger.LogWarning($"Registration with Id: {shipToSite.EaspSepsRegistrationId} not found.");
                return NotFound();
            }
        }


        public async Task<IActionResult> OnPostUpdateCountiesServedAsync()
        {
            _logger.LogInformation("OnPostUpdateCountiesServedAsync called");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                return Page();
            }

            var existingCounties = await _context.EaspSepsRegistrationCounties
                .Where(c => c.EaspSepsRegistrationId == Registration.Id)
                .ToListAsync();

            _context.EaspSepsRegistrationCounties.RemoveRange(existingCounties);

            foreach (var countyId in CountiesServed)
            {
                if (countyId > 0) // assuming 0 means no selection
                {
                    var newCounty = new EaspSepsRegistrationCounty
                    {
                        EaspSepsRegistrationId = Registration.Id,
                        CountyId = countyId
                    };
                    _context.EaspSepsRegistrationCounties.Add(newCounty);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Updated CountiesServed for RegistrationId: {Registration.Id}");
            SuccessMessage = "CountiesServed updated successfully.";

            User = await _userManager.FindByIdAsync(Registration.UserId.ToString());
            var email = User != null ? User.Email : Email;
            if (User == null)
            {
                _logger.LogWarning($"User with Id: {Registration.UserId} not found.");
            }

            return RedirectToPage("/Admin/ManageUsers", new { email = email });
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

