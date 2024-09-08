using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
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
        public List<LkAgencyClassification> AllClassifications { get; set; }

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
                    County = _context.Counties.Where(c => c.county_id == Convert.ToInt32(r.County)).Select(c => c.name).FirstOrDefault(), // Fetch the county name
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
                    CountiesServed = r.CountiesServed.Select(cs => cs.County.name).ToList(),
                    LnkAgencyClassificationData = r.LnkAgencyClassificationData.Select(c => new LnkAgencyClassificationData
                    {
                        Id = c.Id,
                        Category = c.Category,
                        UniqueId = c.UniqueId,
                        AgencyRegistrationId = c.AgencyRegistrationId
                    }).ToList()
                })
                .ToListAsync();

            PendingRegistrations = registrations.Where(r => r.Status == "Pending").ToList();
            ApprovedRegistrations = registrations.Where(r => r.Status == "Approved").ToList();
            DeniedRegistrations = registrations.Where(r => r.Status == "Denied").ToList();

            AllClassifications = await _context.LkAgencyClassifications.Where(c => c.is_active).ToListAsync();
        }

        public async Task<IActionResult> OnPostApproveAsync(int registrationId, string selectedClassifications)
        {
            var registration = await _context.AgencyRegistrations
                .Include(r => r.LnkAgencyClassificationData)
                .FirstOrDefaultAsync(r => r.Id == registrationId);
            if (registration == null)
            {
                return NotFound();
            }

            registration.Status = "Approved";
            registration.LnkAgencyClassificationData.Clear();

            if (!string.IsNullOrEmpty(selectedClassifications))
            {
                var selectedClassificationIds = selectedClassifications.Split(',').Select(int.Parse).ToList();
                var registrationTypes = new List<string>();

                foreach (var classificationId in selectedClassificationIds)
                {
                    var uniqueIdPrefix = classificationId switch
                    {
                        1 => "SP",
                        2 => "ET1",
                        3 => "ET2",
                        4 => "OTH",
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    var uniqueId = $"{uniqueIdPrefix}{registrationId}";
                    var classificationData = new LnkAgencyClassificationData
                    {
                        AgencyRegistrationId = registrationId,
                        Category = classificationId,
                        UniqueId = uniqueId
                    };

                    registration.LnkAgencyClassificationData.Add(classificationData);

                    // Add the corresponding type to the registrationTypes list
                    var type = classificationId switch
                    {
                        1 => "Syringe exchange program",
                        2 => "ESAP Tier 1",
                        3 => "ESAP Tier 2",
                        4 => "Other",
                        _ => null
                    };

                    if (type != null)
                    {
                        registrationTypes.Add(type);
                    }
                }

                // Join all selected types into a single string and assign it to the RegistrationType field
                registration.RegistrationType = string.Join(", ", registrationTypes);
            }

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

        public async Task<IActionResult> OnPostSetPendingAsync(int registrationId)
        {
            var registration = await _context.AgencyRegistrations.FindAsync(registrationId);
            if (registration == null)
            {
                return NotFound();
            }

            registration.Status = "Pending";
            await _context.SaveChangesAsync();

            return RedirectToPage(new { successMessage = "Registration status set to pending successfully!" });
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
        public List<LnkAgencyClassificationData> LnkAgencyClassificationData { get; set; } // Add this line
    }
}
