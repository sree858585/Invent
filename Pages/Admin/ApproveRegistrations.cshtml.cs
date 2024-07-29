using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Admin
{
    public class ApproveRegistrationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ApproveRegistrationsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RegistrationApprovalViewModel> PendingRegistrations { get; set; }
        public List<RegistrationApprovalViewModel> ApprovedRegistrations { get; set; }
        public List<RegistrationApprovalViewModel> DeniedRegistrations { get; set; }

        [BindProperty]
        public string SuccessMessage { get; set; }

        public async Task OnGetAsync(string successMessage = null)
        {
            if (!string.IsNullOrEmpty(successMessage))
            {
                SuccessMessage = successMessage;
            }

            var registrations = await _context.AgencyRegistrations
                .Include(r => r.AgencyContacts)
                .Include(r => r.AdditionalUsers)
                .Include(r => r.ShipInformations)
                .Include(r => r.ShipToSites)
                .Include(r => r.CountiesServed)
                    .ThenInclude(cs => cs.County)
                .Select(r => new RegistrationApprovalViewModel
                {
                    Id = r.Id,
                    AgencyName = r.AgencyName,
                    AlternateName = r.AlternateName,
                    County = r.County,
                    Address = r.Address,
                    Address2 = r.Address2,
                    City = r.City,
                    State = r.State,
                    Zip = r.Zip,
                    RegistrationType = r.RegistrationType,
                    SubmissionDate = r.SubmissionDate,
                    Status = r.Status,
                    AgencyContact = r.AgencyContacts.FirstOrDefault(),
                    AdditionalUsers = r.AdditionalUsers.ToList(),
                    ShipInformation = r.ShipInformations.FirstOrDefault(),
                    AdditionalShipToSites = r.ShipToSites.ToList(),
                    CountiesServed = r.CountiesServed.Select(cs => cs.County.name).ToList()
                })
                .ToListAsync();

            PendingRegistrations = registrations.Where(r => r.Status == "Pending").ToList();
            ApprovedRegistrations = registrations.Where(r => r.Status == "Approved").ToList();
            DeniedRegistrations = registrations.Where(r => r.Status == "Denied").ToList();

            // Debug output
            Console.WriteLine($"Pending Registrations Count: {PendingRegistrations.Count}");
            Console.WriteLine($"Approved Registrations Count: {ApprovedRegistrations.Count}");
            Console.WriteLine($"Denied Registrations Count: {DeniedRegistrations.Count}");
        }


        public async Task<IActionResult> OnPostApproveAsync(int registrationId)
        {
            var registration = await _context.AgencyRegistrations.FindAsync(registrationId);
            if (registration == null)
            {
                return NotFound();
            }

            registration.Status = "Approved";
            await _context.SaveChangesAsync();

            return RedirectToPage(new { successMessage = "Registration approved successfully!" });
        }

        public async Task<IActionResult> OnPostDenyAsync(int registrationId)
        {
            var registration = await _context.AgencyRegistrations.FindAsync(registrationId);
            if (registration == null)
            {
                return NotFound();
            }

            registration.Status = "Denied";
            await _context.SaveChangesAsync();

            return RedirectToPage(new { successMessage = "Registration denied successfully!" });
        }
    }

    public class RegistrationApprovalViewModel
    {
        public int Id { get; set; }
        public string AgencyName { get; set; }
        public string AlternateName { get; set; }
        public string County { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string RegistrationType { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Status { get; set; }
        public AgencyContact AgencyContact { get; set; }
        public List<AdditionalUser> AdditionalUsers { get; set; }
        public ShipInformation ShipInformation { get; set; }
        public List<ShipToSite> AdditionalShipToSites { get; set; }
        public List<string> CountiesServed { get; set; }
    }
}
